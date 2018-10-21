﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeElements.BizRunner;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Orcus.Server.Connection;
using Tasks.Infrastructure.Core;
using Tasks.Infrastructure.Server.Business;
using Tasks.Infrastructure.Server.Data;
using Tasks.Infrastructure.Server.Library;

namespace Tasks.Infrastructure.Server
{
    public class TaskInfo : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource;

        public TaskInfo(OrcusTask orcusTask, Hash hash, bool executeOnServer, bool executeOnClients)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Token = _cancellationTokenSource.Token;

            OrcusTask = orcusTask;
            Hash = hash;
            ExecuteOnServer = executeOnServer;
            ExecuteOnClients = executeOnClients;
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        public OrcusTask OrcusTask { get; }
        public CancellationToken Token { get; }
        public Hash Hash { get; }

        public bool ExecuteOnServer { get; }
        public bool ExecuteOnClients { get; }
    }

    public class OrcusTaskManager
    {
        private readonly ITaskDirectory _taskDirectory;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Guid, TaskInfo> _tasks;
        private readonly AsyncLock _tasksLock;

        public OrcusTaskManager(ITaskDirectory taskDirectory, IServiceProvider serviceProvider)
        {
            _taskDirectory = taskDirectory;
            _serviceProvider = serviceProvider;
            _tasks = new Dictionary<Guid, TaskInfo>();
            ClientTasks = ImmutableList<TaskInfo>.Empty;
            _tasksLock = new AsyncLock();
        }

        public IImmutableList<TaskInfo> ClientTasks { get; private set; }

        public async Task AddTask(OrcusTask orcusTask)
        {
            using (await _tasksLock.LockAsync())
            {
                var hash = _taskDirectory.ComputeTaskHash(orcusTask);

                if (_tasks.TryGetValue(orcusTask.Id, out var taskInfo))
                {
                    if (hash.Equals(taskInfo.Hash)) //the tasks are equal
                        return;

                    throw new InvalidOperationException($"The task with id {orcusTask.Id} already exists. Please update the existing task.");
                }

                var filename = await _taskDirectory.WriteTask(orcusTask);

                //add to database
                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TasksDbContext>();
                    var action = scope.ServiceProvider.GetRequiredService<ICreateOrUpdateTaskAction>();
                    await action.ToRunner(dbContext).ExecuteAsync((orcusTask, filename));

                    if (action.HasErrors)
                        throw new InvalidOperationException(action.Errors.First().ErrorMessage);
                }

                InitializeTask(orcusTask, hash, transmit: true);
            }
        }

        public async Task Initialize()
        {
            using (await _tasksLock.LockAsync())
            {
                var tasks = await _taskDirectory.GetTasks();
                foreach (var task in tasks)
                {
                    var hash = _taskDirectory.ComputeTaskHash(task);
                    InitializeTask(task, hash, transmit: false);
                }
            }
        }

        private void InitializeTask(OrcusTask orcusTask, Hash hash, bool transmit)
        {
            var (executeOnServer, executeOnClient) = GetTaskExecutionMode(orcusTask);

            var taskInfo = new TaskInfo(orcusTask, hash, executeOnServer, executeOnClient);
            if (!_tasks.TryAdd(orcusTask.Id, taskInfo))
            {
                taskInfo.Dispose();
                throw new InvalidOperationException($"Unable to add task with id {orcusTask.Id} because the task already exists.");
            }

            var executionTasks = new List<Task>();
            if (executeOnServer)
            {
                var taskService = new OrcusTaskService(orcusTask, _serviceProvider);
                executionTasks.Add(RunTask(taskService, taskInfo.Token));
            }
            if (executeOnClient)
            {
                if (transmit)
                    executionTasks.Add(TransmitTask(orcusTask, taskInfo.Token));

                ClientTasks = ClientTasks.Add(taskInfo);
            }

            Task.WhenAll(executionTasks).ContinueWith(task => TaskExecutionCompleted(taskInfo));
        }

        private async Task TransmitTask(OrcusTask orcusTask, CancellationToken cancellationToken)
        {

        }

        private async Task RunTask(OrcusTaskService taskService, CancellationToken cancellationToken)
        {
            try
            {
                await taskService.Run(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return; //just ignore
            }
            catch (Exception e)
            {
                taskService.Logger.LogCritical(e, "An error occurred when running task service for task {taskId}.", taskService.OrcusTask.Id);
                return;
            }

            //set finished
        }

        private void TaskExecutionCompleted(TaskInfo taskInfo)
        {
            taskInfo.Dispose();
        }

        private (bool server, bool client) GetTaskExecutionMode(OrcusTask orcusTask)
        {
            var executeOnServer = false;
            var executeOnClient = false;

            using (var scope = _serviceProvider.CreateScope())
            {
                foreach (var orcusTaskCommand in orcusTask.Commands)
                {
                    var serviceType = typeof(ITaskExecutor<>).MakeGenericType(orcusTaskCommand.GetType());
                    var service = scope.ServiceProvider.GetService(serviceType);
                    if (service == null)
                        executeOnClient = true;
                    else executeOnServer = true;
                }
            }

            return (executeOnServer, executeOnClient);
        }
    }
}
﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autofac;
using Orcus.Administration.Library.Services;
using Orcus.Administration.Library.Views;
using Tasks.Infrastructure.Administration.Library;
using Tasks.Infrastructure.Administration.Library.Trigger;
using Tasks.Infrastructure.Administration.Utilities;
using Tasks.Infrastructure.Core;
using Tasks.Infrastructure.Core.Triggers;
using Unclassified.TxLib;

namespace Tasks.Infrastructure.Administration.ViewModels.CreateTask
{
    public class TriggersViewModel : TaskServicesBaseViewModel<ITriggerDescription>
    {
        private readonly IReadOnlyList<ITriggerViewProvider> _viewProviders;

        public TriggersViewModel(IWindowService windowService, IDialogWindow window, IComponentContext container) : base(windowService, window, container)
        {
            _viewProviders = container.Resolve<IEnumerable<ITriggerViewProvider>>().OrderByDescending(x => x.Priority).ToList();
        }

        public override TaskViewModelView CreateView(ITaskServiceDescription description)
        {
            var viewModelType = typeof(ITriggerViewModel<>).MakeGenericType(description.DtoType);
            var viewModel = Container.Resolve(viewModelType);

            UIElement view = null;
            foreach (var viewProvider in _viewProviders)
            {
                view = viewProvider.GetView(viewModel, Container);
                if (view != null)
                    break;
            }

            return new TaskViewModelView(viewModel, view, description, this);
        }

        public override string EntryName { get; } = Tx.T("TasksInfrastructure:CreateTask.Triggers", 1);

        public override void Initialize(OrcusTask orcusTask)
        {
            foreach (var triggerInfo in orcusTask.Triggers)
            {
                var triggerInfoType = triggerInfo.GetType();
                var description = AvailableServices.First(x => x.DtoType == triggerInfoType);
                var view = CreateView(description);

                TaskServiceViewModelUtils.Initialize(view.ViewModel, triggerInfo);
                AddChild(view);
            }
        }

        public override void Apply(OrcusTask orcusTask)
        {
            foreach (var taskView in _childs)
            {
                var dto = TaskServiceViewModelUtils.Build<TriggerInfo>(taskView.ViewModel, new TaskContext());
                orcusTask.Triggers.Add(dto);
            }
        }
    }
}
using System;
using CommunityToolkit.Mvvm.Input;
using SignalGUI.Utils;

namespace SignalGUI.ViewModels;

public partial class CompositeComponentViewModel
{
    [RelayCommand]
    public void SelectSource(SourceItemViewModel source)
    {
        SelectedSource = source;
        SelectedFilter = null; // Deselect filter if source is selected
        UpdateCurrentParameters();
    }

    [RelayCommand]
    public void SelectFilter(FilterItemViewModel filter)
    {
        SelectedFilter = filter;
        SelectedSource = null; // Deselect source if filter is selected
        UpdateCurrentParameters();
    }

    [RelayCommand]
    public void SelectSignalParams()
    {
        SelectedSource = null;
        SelectedFilter = null;
        UpdateCurrentParametersForSignalParams();
    }

    void UpdateCurrentParameters()
    {
        CurrentParameters.Clear();
        if (SelectedSource?.Factory != null)
        {
            var args = SelectedSource.Factory.ConstructorArguments;
            foreach (var param in args)
            {
                var value = args[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value.Type, value.Instance ?? "", (newValue) => {
                    args[param.Key].Instance = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
        else if (SelectedFilter?.Factory != null)
        {
            var args = SelectedFilter.Factory.ConstructorArguments;
            foreach (var param in args)
            {
                var value = args[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value.Type, value.Instance ?? "", (newValue) => {
                    args[param.Key].Instance = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }

    }

    void UpdateCurrentParametersForSignalParams()
    {
        CurrentParameters.Clear();

        if (SignalParams != null)
        {
            var args = SignalParams.ConstructorArguments;
            foreach (var param in SignalParams.ConstructorArguments)
            {
                var value = args[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value.Type, value.Instance ?? "", (newValue) => {
                    args[param.Key].Instance = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
    }
}
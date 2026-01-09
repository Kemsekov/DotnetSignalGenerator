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
            foreach (var param in SelectedSource.Factory.Arguments)
            {
                var value = SelectedSource.Factory.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SelectedSource.Factory.InstanceArguments[param.Key] = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
        else if (SelectedFilter?.Factory != null)
        {
            foreach (var param in SelectedFilter.Factory.Arguments)
            {
                var value = SelectedFilter.Factory.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SelectedFilter.Factory.InstanceArguments[param.Key] = newValue;
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
            foreach (var param in SignalParams.Arguments)
            {
                var value = SignalParams.InstanceArguments[param.Key];
                var paramVM = new ParameterViewModelWithCallback(param.Key, param.Value, value, (newValue) => {
                    SignalParams.InstanceArguments[param.Key] = newValue;
                });
                CurrentParameters.Add(paramVM);
            }
        }
    }
}
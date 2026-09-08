// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

// ReSharper disable once CheckNamespace
namespace Wpf.Ui.Controls;

internal class RatingControlAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly RatingControl _owner;

    public RatingControlAutomationPeer(RatingControl owner)
        : base(owner)
    {
        _owner = owner;
    }

    public bool IsReadOnly => !_owner.IsEnabled;

    public double LargeChange => _owner.HalfStarEnabled ? 0.5D : 1.0D;

    public double SmallChange => _owner.HalfStarEnabled ? 0.5D : 1.0D;

    public double Maximum => _owner.MaxRating;

    public double Minimum => 0.0D;

    public double Value => _owner.Value;

    protected override string GetClassNameCore()
    {
        return nameof(RatingControl);
    }

    protected override AutomationControlType GetAutomationControlTypeCore()
    {
        return AutomationControlType.Slider;
    }

    public override object GetPattern(PatternInterface patternInterface)
    {
        if (patternInterface == PatternInterface.RangeValue)
        {
            return this;
        }

        return base.GetPattern(patternInterface);
    }

    public void SetValue(double value)
    {
        _owner.SetCurrentValue(RatingControl.ValueProperty, value);
    }

    internal void NotifyValueChanged(double oldValue, double newValue)
    {
        RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, oldValue, newValue);
    }
}

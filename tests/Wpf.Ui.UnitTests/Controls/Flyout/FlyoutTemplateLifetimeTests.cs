// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Controls;

public class FlyoutTemplateLifetimeTests
{
    private const string ElementPopup = "PART_Popup";

    [Fact]
    public void OnApplyTemplate_TemplateWithoutPopupPart_TheRetainedOldPopupNoLongerAffectsFlyoutState()
    {
        WpfTestHost.Invoke(() =>
        {
            var flyout = new Flyout { Template = BuildTemplateWithPopup() };
            flyout.ApplyTemplate();
            var oldPopup = (RaisablePopup)flyout.Template.FindName(ElementPopup, flyout)!;

            var openedCount = 0;
            var closedCount = 0;
            flyout.Opened += (_, _) => openedCount++;
            flyout.Closed += (_, _) => closedCount++;

            flyout.Template = BuildTemplateWithoutPopup();
            flyout.ApplyTemplate();

            oldPopup.RaiseOpenedForTest();
            oldPopup.RaiseClosedForTest();

            Assert.Equal(0, openedCount);
            Assert.Equal(0, closedCount);
            Assert.False(flyout.IsOpen);
        });
    }

    [Fact]
    public void OnApplyTemplate_Reapplied_OnlyTheNewPopupRaisesFlyoutEvents()
    {
        WpfTestHost.Invoke(() =>
        {
            var flyout = new Flyout { Template = BuildTemplateWithPopup() };
            flyout.ApplyTemplate();
            var oldPopup = (RaisablePopup)flyout.Template.FindName(ElementPopup, flyout)!;

            flyout.Template = BuildTemplateWithPopup();
            flyout.ApplyTemplate();
            var newPopup = (RaisablePopup)flyout.Template.FindName(ElementPopup, flyout)!;

            Assert.NotSame(oldPopup, newPopup);

            var openedCount = 0;
            flyout.Opened += (_, _) => openedCount++;

            oldPopup.RaiseOpenedForTest();
            Assert.Equal(0, openedCount);

            newPopup.RaiseOpenedForTest();
            Assert.Equal(1, openedCount);
        });
    }

    [Fact]
    public void OnApplyTemplate_TemplateWithoutPopupPart_BoundIsOpenSurvivesStalePopupClosedCallback()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new IsOpenSource { Value = true };
            var flyout = new Flyout { Template = BuildTemplateWithPopup() };
            _ = BindingOperations.SetBinding(
                flyout,
                Flyout.IsOpenProperty,
                new Binding(nameof(IsOpenSource.Value)) { Source = source });

            flyout.ApplyTemplate();
            var oldPopup = (RaisablePopup)flyout.Template.FindName(ElementPopup, flyout)!;

            flyout.Template = BuildTemplateWithoutPopup();
            flyout.ApplyTemplate();

            oldPopup.RaiseClosedForTest();

            Assert.True(flyout.IsOpen);
            Assert.NotNull(BindingOperations.GetBindingExpression(flyout, Flyout.IsOpenProperty));
        });
    }

    [Fact]
    public void ShowThenHide_WithARealPopup_OpensAndClosesExactlyOnceEach()
    {
        WpfTestHost.Invoke(() =>
        {
            Window owner = WpfTestHost.CreateOffscreenOwnerWindow();
            var flyout = new Flyout { Template = BuildRealPopupTemplate() };
            ((Grid)owner.Content).Children.Add(flyout);
            owner.Show();
            flyout.ApplyTemplate();
            var popup = (Popup)flyout.Template.FindName(ElementPopup, flyout)!;

            var openedCount = 0;
            var closedCount = 0;
            flyout.Opened += (_, _) => openedCount++;
            flyout.Closed += (_, _) => closedCount++;

            flyout.Show();
            PumpDispatcherUntil(() => openedCount > 0);
            WindowActivationGuard.AssertPopupCompliant(popup);
            Assert.True(flyout.IsOpen);
            Assert.Equal(1, openedCount);

            flyout.Hide();
            PumpDispatcherUntil(() => closedCount > 0);
            Assert.False(flyout.IsOpen);
            Assert.Equal(1, closedCount);

            owner.Close();
        });
    }

    private static void PumpDispatcherUntil(Func<bool> condition)
    {
        const int maxDispatcherYields = 500;

        for (var yields = 0; !condition(); yields++)
        {
            if (yields >= maxDispatcherYields)
            {
                throw new InvalidOperationException("Dispatcher did not reach the expected state.");
            }

            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }
    }

    private static ControlTemplate BuildTemplateWithPopup()
    {
        const string xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              xmlns:test="clr-namespace:Wpf.Ui.UnitTests.Controls;assembly=Wpf.Ui.UnitTests"
                              TargetType="{x:Type controls:Flyout}">
                <Grid>
                    <test:RaisablePopup x:Name="PART_Popup">
                        <ContentPresenter Content="{TemplateBinding Content}" />
                    </test:RaisablePopup>
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(xaml);
    }

    private static ControlTemplate BuildTemplateWithoutPopup()
    {
        const string xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              TargetType="{x:Type controls:Flyout}">
                <Grid>
                    <ContentPresenter Content="{TemplateBinding Content}" />
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(xaml);
    }

    private static ControlTemplate BuildRealPopupTemplate()
    {
        const string xaml = """
            <ControlTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:controls="clr-namespace:Wpf.Ui.Controls;assembly=Wpf.Ui"
                              TargetType="{x:Type controls:Flyout}">
                <Grid>
                    <Popup x:Name="PART_Popup" IsOpen="{TemplateBinding IsOpen}" AllowsTransparency="True">
                        <ContentPresenter Content="{TemplateBinding Content}" />
                    </Popup>
                </Grid>
            </ControlTemplate>
            """;

        return (ControlTemplate)XamlReader.Parse(xaml);
    }

    private sealed class IsOpenSource
    {
        public bool Value { get; set; }
    }
}

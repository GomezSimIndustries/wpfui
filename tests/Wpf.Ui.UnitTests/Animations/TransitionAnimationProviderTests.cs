// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.ComponentModel;
using System.Threading;
using System.Windows.Data;
using System.Windows.Media;
using Wpf.Ui.Animations;
using Wpf.Ui.UnitTests.TestHelpers;

namespace Wpf.Ui.UnitTests.Animations;

public class TransitionAnimationProviderTests
{
    [Fact]
    public void ApplyTransition_ReturnsFalse_WhenDurationIsLessThan10()
    {
        UIElement mockedUiElement = Substitute.For<UIElement>();

        var result = TransitionAnimationProvider.ApplyTransition(mockedUiElement, Transition.FadeIn, -10);

        Assert.False(result);
    }

    [Fact]
    public void ApplyTransition_ReturnsFalse_WhenElementIsNull()
    {
        var result = TransitionAnimationProvider.ApplyTransition(null, Transition.FadeIn, 1000);

        Assert.False(result);
    }

    [Fact]
    public void ApplyTransition_SlideLeft_PreservesExistingGroupTransformAndOrigin()
    {
        WpfTestHost.Invoke(() =>
        {
            var originalGroup = new TransformGroup();
            originalGroup.Children.Add(new ScaleTransform(2, 3));
            originalGroup.Children.Add(new RotateTransform(15));

            var element = new FrameworkElement
            {
                RenderTransform = originalGroup,
                RenderTransformOrigin = new Point(0.2, 0.8),
            };

            var applied = TransitionAnimationProvider.ApplyTransition(element, Transition.SlideLeft, 1000);

            Assert.True(applied);
            Assert.Equal(new Point(0.2, 0.8), element.RenderTransformOrigin);

            var composed = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(originalGroup, composed.Children[0]);
            Assert.IsType<TranslateTransform>(composed.Children[1]);
        });
    }

    [Fact]
    public void ApplyTransition_SlideRight_PreservesRenderTransformBinding()
    {
        WpfTestHost.Invoke(() =>
        {
            var source = new ScaleTransform(1.5, 1.5);
            var element = new FrameworkElement { DataContext = source };

            BindingOperations.SetBinding(
                element,
                UIElement.RenderTransformProperty,
                new Binding { Path = new PropertyPath(".") }
            );

            Assert.Same(source, element.RenderTransform);

            var applied = TransitionAnimationProvider.ApplyTransition(element, Transition.SlideRight, 1000);

            Assert.True(applied);

            var composed = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(source, composed.Children[0]);

            var expression = BindingOperations.GetBindingExpression(element, UIElement.RenderTransformProperty);
            Assert.NotNull(expression);
        });
    }

    [Fact]
    public void ApplyTransition_SlideBottom_ToleratesFrozenTransform()
    {
        WpfTestHost.Invoke(() =>
        {
            var frozen = new ScaleTransform(1.2, 1.2);
            frozen.Freeze();

            var element = new FrameworkElement { RenderTransform = frozen };

            var applied = TransitionAnimationProvider.ApplyTransition(element, Transition.SlideBottom, 1000);

            Assert.True(applied);

            var composed = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(frozen, composed.Children[0]);
        });
    }

    [Fact]
    public void ApplyTransition_FadeInWithSlide_ToleratesSharedTransformInstance()
    {
        WpfTestHost.Invoke(() =>
        {
            var shared = new ScaleTransform(1.1, 1.1);
            var elementA = new FrameworkElement { RenderTransform = shared };
            var elementB = new FrameworkElement { RenderTransform = shared };

            var applied = TransitionAnimationProvider.ApplyTransition(elementA, Transition.FadeInWithSlide, 1000);

            Assert.True(applied);
            Assert.Same(shared, elementB.RenderTransform);

            var composed = Assert.IsType<TransformGroup>(elementA.RenderTransform);
            Assert.Same(shared, composed.Children[0]);
        });
    }

    [Fact]
    public void ApplyTransition_SlideLeft_PreservesIdentityTransform()
    {
        WpfTestHost.Invoke(() =>
        {
            var element = new FrameworkElement();

            var applied = TransitionAnimationProvider.ApplyTransition(element, Transition.SlideLeft, 1000);

            Assert.True(applied);

            var composed = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(Transform.Identity, composed.Children[0]);
        });
    }

    [Fact]
    public void ApplyTransition_SlideRight_PreservesExistingTranslateTransform()
    {
        WpfTestHost.Invoke(() =>
        {
            var existingTranslate = new TranslateTransform(4, 5);
            var element = new FrameworkElement { RenderTransform = existingTranslate };

            var applied = TransitionAnimationProvider.ApplyTransition(element, Transition.SlideRight, 1000);

            Assert.True(applied);

            var composed = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Same(existingTranslate, composed.Children[0]);
            Assert.Equal(4, existingTranslate.X);
            Assert.Equal(5, existingTranslate.Y);
        });
    }

    [Fact]
    public void ApplyTransition_SlideLeft_RestoresOriginalTransformAfterCompletion()
    {
        var (window, element, originalTransform) = WpfTestHost.Invoke(() =>
        {
            var rotate = new RotateTransform(15);
            var hostedElement = new FrameworkElement { RenderTransform = rotate };
            var hostWindow = CreateShownHostWindow(hostedElement);

            return (hostWindow, (UIElement)hostedElement, (Transform)rotate);
        });

        try
        {
            var applied = WpfTestHost.Invoke(
                () => TransitionAnimationProvider.ApplyTransition(element, Transition.SlideLeft, 15)
            );

            Assert.True(applied);

            var finalTransform = AwaitTransformRestoration(element, originalTransform);

            Assert.Same(originalTransform, finalTransform);
        }
        finally
        {
            WpfTestHost.Invoke(window.Close);
        }
    }

    [Fact]
    public void ApplyTransition_RapidRepeatedNavigation_RestoresOriginalTransformExactlyOnce()
    {
        var (window, element, originalTransform) = WpfTestHost.Invoke(() =>
        {
            var scale = new ScaleTransform(1.3, 1.3);
            var hostedElement = new FrameworkElement { RenderTransform = scale };
            var hostWindow = CreateShownHostWindow(hostedElement);

            return (hostWindow, (UIElement)hostedElement, (Transform)scale);
        });

        try
        {
            WpfTestHost.Invoke(() =>
            {
                Assert.True(TransitionAnimationProvider.ApplyTransition(element, Transition.SlideLeft, 15));
                Assert.True(TransitionAnimationProvider.ApplyTransition(element, Transition.SlideRight, 15));
            });

            var finalTransform = AwaitTransformRestoration(element, originalTransform);

            Assert.Same(originalTransform, finalTransform);
        }
        finally
        {
            WpfTestHost.Invoke(window.Close);
        }
    }

    /// <summary>Creates and shows an off-screen window hosting <paramref name="content"/>, whose live composition target drives real animation ticks.</summary>
    private static Window CreateShownHostWindow(UIElement content)
    {
        var window = new Window
        {
            Content = content,
            Width = 200,
            Height = 100,
            Left = -32000,
            Top = -32000,
            WindowStyle = WindowStyle.None,
            ShowInTaskbar = false,
            ShowActivated = false,
        };

        window.Show();

        return window;
    }

    /// <summary>Blocks the calling thread until <paramref name="element"/>'s RenderTransform reverts to <paramref name="originalTransform"/>, bounded by a hang guard.</summary>
    private static Transform AwaitTransformRestoration(UIElement element, Transform originalTransform)
    {
        var descriptor = DependencyPropertyDescriptor.FromProperty(
            UIElement.RenderTransformProperty,
            typeof(UIElement)
        )!;
        using var restored = new ManualResetEventSlim(false);

        void OnRenderTransformChanged(object? sender, EventArgs e)
        {
            if (ReferenceEquals(element.RenderTransform, originalTransform))
            {
                restored.Set();
            }
        }

        WpfTestHost.Invoke(() => descriptor.AddValueChanged(element, OnRenderTransformChanged));

        try
        {
            Assert.True(
                restored.Wait(TimeSpan.FromSeconds(10)),
                "Timed out waiting for the transition to restore the original transform."
            );
        }
        finally
        {
            WpfTestHost.Invoke(() => descriptor.RemoveValueChanged(element, OnRenderTransformChanged));
        }

        return WpfTestHost.Invoke(() => element.RenderTransform);
    }
}

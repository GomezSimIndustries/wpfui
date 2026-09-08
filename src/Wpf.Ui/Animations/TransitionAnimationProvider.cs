// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Leszek Pomianowski and WPF UI Contributors.
// All Rights Reserved.

using System.Runtime.CompilerServices;
using System.Windows.Media.Animation;
using Wpf.Ui.Hardware;

namespace Wpf.Ui.Animations;

/// <summary>
/// Provides tools for <see cref="FrameworkElement"/> animation.
/// </summary>
/// <example>
/// <code lang="csharp">
/// TransitionAnimationProvider.ApplyTransition(MyFrameworkElement, Transition.FadeIn, 500);
/// </code>
/// </example>
public static class TransitionAnimationProvider
{
    private const double DecelerationRatio = 0.7D;

    private static readonly ConditionalWeakTable<UIElement, TransientTranslationState> TransientTranslations =
        new();

    /// <summary>
    /// Attempts to apply an animation effect while adding content to the frame.
    /// </summary>
    /// <param name="element">Currently rendered element.</param>
    /// <param name="type">Selected transition type.</param>
    /// <param name="duration">Transition duration.</param>
    /// <returns>Returns <see langword="true"/> if the transition was applied. Otherwise <see langword="false"/>.</returns>
    public static bool ApplyTransition(object? element, Transition type, int duration)
    {
        if (
            type == Transition.None
            || !HardwareAcceleration.IsSupported(RenderingTier.PartialAcceleration)
            || element is not UIElement uiElement
            || duration < 10
        )
        {
            return false;
        }

        duration = duration > 10000 ? 10000 : duration;

        var timespanDuration = new Duration(TimeSpan.FromMilliseconds(duration));

        switch (type)
        {
            case Transition.FadeIn:
                FadeInTransition(uiElement, timespanDuration);
                break;

            case Transition.FadeInWithSlide:
                FadeInWithSlideTransition(uiElement, timespanDuration);
                break;

            case Transition.SlideBottom:
                SlideBottomTransition(uiElement, timespanDuration);
                break;

            case Transition.SlideRight:
                SlideRightTransition(uiElement, timespanDuration);
                break;

            case Transition.SlideLeft:
                SlideLeftTransition(uiElement, timespanDuration);
                break;

            default:
                return false;
        }

        return true;
    }

    private static void FadeInTransition(UIElement animatedUiElement, Duration duration)
    {
        var opacityDoubleAnimation = new DoubleAnimation
        {
            Duration = duration,
            DecelerationRatio = DecelerationRatio,
            From = 0.0,
            To = 1.0,
        };

        animatedUiElement.BeginAnimation(UIElement.OpacityProperty, opacityDoubleAnimation);
    }

    private static void FadeInWithSlideTransition(UIElement animatedUiElement, Duration duration)
    {
        AnimateTransientTranslate(animatedUiElement, TranslateTransform.YProperty, 30, duration);

        var opacityDoubleAnimation = new DoubleAnimation
        {
            Duration = duration,
            DecelerationRatio = DecelerationRatio,
            From = 0.0,
            To = 1.0,
        };

        animatedUiElement.BeginAnimation(UIElement.OpacityProperty, opacityDoubleAnimation);
    }

    private static void SlideBottomTransition(UIElement animatedUiElement, Duration duration) =>
        AnimateTransientTranslate(animatedUiElement, TranslateTransform.YProperty, 30, duration);

    private static void SlideRightTransition(UIElement animatedUiElement, Duration duration) =>
        AnimateTransientTranslate(animatedUiElement, TranslateTransform.XProperty, 50, duration);

    private static void SlideLeftTransition(UIElement animatedUiElement, Duration duration) =>
        AnimateTransientTranslate(animatedUiElement, TranslateTransform.XProperty, -50, duration);

    /// <summary>
    /// Animates a transient offset on <paramref name="axisProperty"/> without disturbing the element's own <see cref="UIElement.RenderTransform"/>, restoring it once the animation completes or is superseded.
    /// </summary>
    private static void AnimateTransientTranslate(
        UIElement animatedUiElement,
        DependencyProperty axisProperty,
        double from,
        Duration duration
    )
    {
        var translate = BeginTransientTranslate(animatedUiElement, out var token);

        var translateDoubleAnimation = new DoubleAnimation
        {
            Duration = duration,
            DecelerationRatio = DecelerationRatio,
            From = from,
            To = 0,
        };

        translateDoubleAnimation.Completed += (_, _) => EndTransientTranslate(animatedUiElement, token);

        translate.BeginAnimation(axisProperty, translateDoubleAnimation);
    }

    /// <summary>
    /// Wraps the element's current <see cref="UIElement.RenderTransform"/> in a group with an owned <see cref="TranslateTransform"/>, capturing the original transform for later restoration.
    /// </summary>
    /// <returns>The owned <see cref="TranslateTransform"/> to animate.</returns>
    private static TranslateTransform BeginTransientTranslate(UIElement animatedUiElement, out int token)
    {
        if (TransientTranslations.TryGetValue(animatedUiElement, out var existingState))
        {
            token = ++existingState.Token;
            return existingState.Translate;
        }

        var baseTransform = animatedUiElement.RenderTransform ?? Transform.Identity;
        var translate = new TranslateTransform(0, 0);
        var group = new TransformGroup();
        group.Children.Add(baseTransform);
        group.Children.Add(translate);

        var state = new TransientTranslationState { BaseTransform = baseTransform, Translate = translate };
        token = state.Token;

        TransientTranslations.Add(animatedUiElement, state);
        animatedUiElement.SetCurrentValue(UIElement.RenderTransformProperty, group);

        return translate;
    }

    /// <summary>
    /// Restores the element's original <see cref="UIElement.RenderTransform"/> once the transition identified by <paramref name="token"/> is the most recent one applied.
    /// </summary>
    private static void EndTransientTranslate(UIElement animatedUiElement, int token)
    {
        if (
            !TransientTranslations.TryGetValue(animatedUiElement, out var state)
            || state.Token != token
        )
        {
            return;
        }

        TransientTranslations.Remove(animatedUiElement);
        animatedUiElement.SetCurrentValue(UIElement.RenderTransformProperty, state.BaseTransform);
    }

    private sealed class TransientTranslationState
    {
        public required Transform BaseTransform { get; init; }

        public required TranslateTransform Translate { get; init; }

        public int Token { get; set; } = 1;
    }
}

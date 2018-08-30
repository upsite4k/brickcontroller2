﻿using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BrickController2.HardwareServices;
using BrickController2.UI.Services;
using CoreGraphics;
using UIKit;

namespace BrickController2.iOS.UI.Services
{
    public class DialogService : IDialogService
    {
        private readonly IGameControllerService _gameControllerService;

        public DialogService(IGameControllerService gameControllerService)
        {
            _gameControllerService = gameControllerService;
        }

        public Task ShowMessageBoxAsync(string title, string message, string buttonText)
        {
            var completionSource = new TaskCompletionSource<bool>();
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            alert.AddAction(UIAlertAction.Create(buttonText ?? "Ok", UIAlertActionStyle.Default, action =>
            {
                completionSource.SetResult(true);
            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);

            return completionSource.Task;
        }

        public Task<bool> ShowQuestionDialogAsync(string title, string message, string positiveButtonText, string negativeButtonText)
        {
            var completionSource = new TaskCompletionSource<bool>();
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            alert.AddAction(UIAlertAction.Create(positiveButtonText ?? "Ok", UIAlertActionStyle.Default, action =>
            {
                completionSource.SetResult(true);
            }));

            alert.AddAction(UIAlertAction.Create(negativeButtonText ?? "Cancel", UIAlertActionStyle.Cancel, action =>
            {
                completionSource.SetResult(false);
            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);

            return completionSource.Task;
        }

        public Task<InputDialogResult> ShowInputDialogAsync(string title, string message, string initialValue, string placeHolder, string positiveButtonText, string negativeButtonText)
        {
            var completionSource = new TaskCompletionSource<InputDialogResult>();
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            alert.AddTextField(textField =>
            {
                textField.Text = initialValue ?? string.Empty;
                textField.Placeholder = placeHolder ?? string.Empty;
            });

            alert.AddAction(UIAlertAction.Create(positiveButtonText ?? "Ok", UIAlertActionStyle.Default, action =>
            {
                completionSource.SetResult(new InputDialogResult(true, alert.TextFields.First().Text));
            }));

            alert.AddAction(UIAlertAction.Create(negativeButtonText ?? "Cancel", UIAlertActionStyle.Cancel, action =>
            {
                completionSource.SetResult(new InputDialogResult(false, alert.TextFields.First().Text));
            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);

            return completionSource.Task;
        }

        public IProgress ShowProgressDialog(bool isDeterministic, string title, string message, string cancelButtonText, CancellationTokenSource tokenSource)
        {
            // TODO: make the alert view bigger somehow
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            UIProgressView progressView = null;
            if (isDeterministic)
            {
                progressView = new UIProgressView(new CGRect(30F, 80F, 225F, 90F));
                progressView.Style = UIProgressViewStyle.Bar;
                progressView.Progress = 0.0F;
                alert.View.AddSubview(progressView);
            }
            else
            {
                var activityIndicator = new UIActivityIndicatorView(UIActivityIndicatorViewStyle.Gray);
                activityIndicator.Frame = new CGRect(121F, 60F, 37F, 37F);
                activityIndicator.StartAnimating();
                alert.View.AddSubview(activityIndicator);
            }

            if (tokenSource != null)
            {
                alert.AddAction(UIAlertAction.Create(cancelButtonText ?? "Cancel", UIAlertActionStyle.Cancel, action =>
                {
                    tokenSource.Cancel();
                }));
            }

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);

            return new ProgressImpl(alert, progressView);
        }

        public Task<GameControllerEventDialogResult> ShowGameControllerEventDialogAsync(string title, string message, string cancelButtonText)
        {
            var completionSource = new TaskCompletionSource<GameControllerEventDialogResult>();
            var alert = UIAlertController.Create(title, message, UIAlertControllerStyle.Alert);

            _gameControllerService.GameControllerEvent += GameControllerEventHandler;

            alert.AddAction(UIAlertAction.Create(cancelButtonText ?? "Cancel", UIAlertActionStyle.Cancel, action =>
            {
                _gameControllerService.GameControllerEvent -= GameControllerEventHandler;
                completionSource.SetResult(new GameControllerEventDialogResult(false, GameControllerEventType.Button, null));
            }));

            UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(alert, true, null);

            return completionSource.Task;

            async void GameControllerEventHandler(object sender, GameControllerEventArgs args)
            {
                var controllerEvent = args.ControllerEvents.First();
                if (controllerEvent.Key.EventType == GameControllerEventType.Button && 0.0F < controllerEvent.Value)
                {
                    return;
                }

                _gameControllerService.GameControllerEvent -= GameControllerEventHandler;
                await alert.DismissViewControllerAsync(true);
                completionSource.SetResult(new GameControllerEventDialogResult(true, controllerEvent.Key.EventType, controllerEvent.Key.EventCode));
            }
        }
    }
}
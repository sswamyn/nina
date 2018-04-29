﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NINACustomControlLibrary {

    public class CancellableButton : UserControl {

        static CancellableButton() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CancellableButton), new FrameworkPropertyMetadata(typeof(CancellableButton)));
        }

        public static readonly DependencyProperty ButtonForegroundBrushProperty =
           DependencyProperty.Register(nameof(ButtonForegroundBrush), typeof(Brush), typeof(CancellableButton), new UIPropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush ButtonForegroundBrush {
            get {
                return (Brush)GetValue(ButtonForegroundBrushProperty);
            }
            set {
                SetValue(ButtonForegroundBrushProperty, value);
            }
        }

        //public static readonly DependencyProperty ButtonTooltipProperty =
        //    DependencyProperty.Register(nameof(ButtonTooltip), typeof(string), typeof(CancellableButton), new UIPropertyMetadata(null));

        //public string ButtonTooltip {
        //    get {
        //        return (string)GetValue(ButtonTooltipProperty);
        //    }
        //    set {
        //        SetValue(ButtonTooltipProperty, value);
        //    }
        //}

        public static readonly DependencyProperty ButtonTextProperty =
            DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(CancellableButton), new UIPropertyMetadata(null));

        public string ButtonText {
            get {
                return (string)GetValue(ButtonTextProperty);
            }
            set {
                SetValue(ButtonTextProperty, value);
            }
        }

        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(CancellableButton), new UIPropertyMetadata(null));

        public Style ButtonStyle {
            get {
                return (Style)GetValue(ButtonStyleProperty);
            }
            set {
                SetValue(ButtonStyleProperty, value);
            }
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(CancellableButton), new UIPropertyMetadata(null));

        public ICommand Command {
            get {
                return (ICommand)GetValue(CommandProperty);
            }
            set {
                SetValue(CommandProperty, value);
            }
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(CancellableButton), new UIPropertyMetadata(null));

        public ICommand CancelCommand {
            get {
                return (ICommand)GetValue(CancelCommandProperty);
            }
            set {
                SetValue(CancelCommandProperty, value);
            }
        }

        public static readonly DependencyProperty ButtonImageProperty =
           DependencyProperty.Register(nameof(ButtonImage), typeof(Geometry), typeof(CancellableButton), new UIPropertyMetadata(null));

        public Geometry ButtonImage {
            get {
                return (Geometry)GetValue(ButtonImageProperty);
            }
            set {
                SetValue(ButtonImageProperty, value);
            }
        }

        public static readonly DependencyProperty CancelButtonImageProperty =
           DependencyProperty.Register(nameof(CancelButtonImage), typeof(Geometry), typeof(CancellableButton), new UIPropertyMetadata(null));

        public Geometry CancelButtonImage {
            get {
                return (Geometry)GetValue(CancelButtonImageProperty);
            }
            set {
                SetValue(CancelButtonImageProperty, value);
            }
        }
    }
}
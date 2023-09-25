﻿#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility.Extensions;
using NINA.Sequencer.SequenceItem;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NINA.View.Sequencer.MiniSequencer {

    /// <summary>
    /// Interaction logic for MiniSequencer.xaml
    /// </summary>
    public partial class MiniSequencer : UserControl {

        public MiniSequencer() {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            var sw = Stopwatch.StartNew();
            if(e.OriginalSource is TreeView source) {
                if (e.NewValue is ISequenceItem item && item.Status == Core.Enum.SequenceEntityStatus.RUNNING) {
                    if(source.ItemContainerGenerator.ContainerFromItemRecursive(item) is TreeViewItem treeViewItem) { 
                        treeViewItem.BringIntoView();
                    }

                }
            }
            Debug.Print(sw.Elapsed.ToString());
        }
    }
}
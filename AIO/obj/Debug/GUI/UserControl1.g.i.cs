﻿#pragma checksum "..\..\..\GUI\UserControl1.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "F7DB84064E6C93BBAC5ED9F19461DCDFCFCE0911193862144D6920BAD2AF3F65"
//------------------------------------------------------------------------------
// <auto-generated>
//     Ce code a été généré par un outil.
//     Version du runtime :4.0.30319.42000
//
//     Les modifications apportées à ce fichier peuvent provoquer un comportement incorrect et seront perdues si
//     le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using WholesomeTBCAIO.GUI;


namespace WholesomeTBCAIO.GUI {
    
    
    /// <summary>
    /// AIOSettingsControl
    /// </summary>
    public partial class AIOSettingsControl : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 62 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown ServerRate;
        
        #line default
        #line hidden
        
        
        #line 78 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.ToggleSwitch LogDebug;
        
        #line default
        #line hidden
        
        
        #line 94 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.ToggleSwitch Autofarm;
        
        #line default
        #line hidden
        
        
        #line 110 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.NumericUpDown BroadcasterInterval;
        
        #line default
        #line hidden
        
        
        #line 126 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.ToggleSwitch CraftWhileFarming;
        
        #line default
        #line hidden
        
        
        #line 142 "..\..\..\GUI\UserControl1.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal MahApps.Metro.Controls.ToggleSwitch FilterLoot;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/Wholesome_TBC_AIO_Fightclasses;component/gui/usercontrol1.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\GUI\UserControl1.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.ServerRate = ((MahApps.Metro.Controls.NumericUpDown)(target));
            
            #line 64 "..\..\..\GUI\UserControl1.xaml"
            this.ServerRate.MouseLeave += new System.Windows.Input.MouseEventHandler(this.ServerRateChanged);
            
            #line default
            #line hidden
            return;
            case 2:
            this.LogDebug = ((MahApps.Metro.Controls.ToggleSwitch)(target));
            
            #line 83 "..\..\..\GUI\UserControl1.xaml"
            this.LogDebug.MouseLeave += new System.Windows.Input.MouseEventHandler(this.LogDebugChanged);
            
            #line default
            #line hidden
            return;
            case 3:
            this.Autofarm = ((MahApps.Metro.Controls.ToggleSwitch)(target));
            
            #line 99 "..\..\..\GUI\UserControl1.xaml"
            this.Autofarm.MouseLeave += new System.Windows.Input.MouseEventHandler(this.AutofarmChanged);
            
            #line default
            #line hidden
            return;
            case 4:
            this.BroadcasterInterval = ((MahApps.Metro.Controls.NumericUpDown)(target));
            
            #line 112 "..\..\..\GUI\UserControl1.xaml"
            this.BroadcasterInterval.MouseLeave += new System.Windows.Input.MouseEventHandler(this.BroadcasterIntervalChanged);
            
            #line default
            #line hidden
            return;
            case 5:
            this.CraftWhileFarming = ((MahApps.Metro.Controls.ToggleSwitch)(target));
            
            #line 131 "..\..\..\GUI\UserControl1.xaml"
            this.CraftWhileFarming.MouseLeave += new System.Windows.Input.MouseEventHandler(this.CraftWhileFarmingChanged);
            
            #line default
            #line hidden
            return;
            case 6:
            this.FilterLoot = ((MahApps.Metro.Controls.ToggleSwitch)(target));
            
            #line 147 "..\..\..\GUI\UserControl1.xaml"
            this.FilterLoot.MouseLeave += new System.Windows.Input.MouseEventHandler(this.FilterLootChanged);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}


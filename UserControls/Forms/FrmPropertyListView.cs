﻿using CaeGlobals;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace UserControls
{
    public partial class FrmPropertyListView : FrmProperties
    {
        // Variables                                                                                                                
        protected int _preselectIndex;
        //
        protected bool _firstTime;
        protected int _maxTopLvHeight;
        protected int _minTopLvHeight;
        protected int _initialFormHeight;
        protected int _initialTopGbHeight;
        protected int _initialTopLvHeight;
        protected int _initialBetweenHeight;
        protected int _initialBottomGbHeight;
        

        // Constructors                                                                                                             
        public FrmPropertyListView()
            : this(2.0)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="labelRatio">Larger value means wider second column. Default = 2.0</param>
        public FrmPropertyListView(double labelRatio)
            : base(labelRatio)
        {
            InitializeComponent();
            //
            _preselectIndex = -1;
            _firstTime = true;
        }


        // Event handlers                                                                                                           
        private void FrmPropertyListView_Resize(object sender, EventArgs e)
        {
            if (!_firstTime)
            {
                int delta = Height - _initialFormHeight;
                int deltaTop = delta;
                if (_initialTopLvHeight + deltaTop > _maxTopLvHeight) deltaTop = _maxTopLvHeight - _initialTopLvHeight;
                if (_initialTopLvHeight + deltaTop < _minTopLvHeight) deltaTop = _minTopLvHeight - _initialTopLvHeight;
                int deltaBottom = delta - deltaTop;
                //
                gbType.Height = _initialTopGbHeight + deltaTop;
                gbProperties.Top = gbType.Bottom + _initialBetweenHeight;
                gbProperties.Height = _initialBottomGbHeight + deltaBottom;
            }
        }
        private void FrmPropertyListView_Shown(object sender, EventArgs e)
        {
            if (_firstTime)
            {
                _maxTopLvHeight = 0;
                foreach (ListViewItem item in lvTypes.Items) _maxTopLvHeight += item.Bounds.Height;
                _minTopLvHeight = _maxTopLvHeight / lvTypes.Items.Count * 3;        // show at least three items
                _maxTopLvHeight += 4;
                _minTopLvHeight += 4;
                _maxTopLvHeight = Math.Max(_maxTopLvHeight, lvTypes.Height);
                //
                _initialFormHeight = Height;
                _initialTopGbHeight = gbType.Height;
                _initialTopLvHeight = lvTypes.Height;
                _initialBetweenHeight = gbProperties.Top - gbType.Bottom;
                _initialBottomGbHeight = gbProperties.Height;
                //
                _firstTime = false;
            }
        }
        private void FrmPropertyListView_VisibleChanged(object sender, EventArgs e)
        {
            if (Visible)
            {
                OnListViewTypeSelectedIndexChanged();
            }
        }
        private void lvTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnListViewTypeSelectedIndexChanged();
            //
            if (lvTypes.SelectedItems != null && lvTypes.SelectedItems.Count == 1)
            {
                ListViewItem listViewItem = lvTypes.SelectedItems[0];
                lvTypes.EnsureVisible(listViewItem.Index);
            }
        }
        private void lvTypes_MouseUp(object sender, MouseEventArgs e)
        {
            OnListViewTypeMouseUp();
        }


        // Methods                                                                                                                  
        public override bool PrepareForm(string stepName, string itemToEditName)
        {
            lvTypes.Enabled = true;
            //
            bool result = base.PrepareForm(stepName, itemToEditName);
            //
            PreselectListViewItem(_preselectIndex);
            //
            return result;
        }
        protected virtual void OnListViewTypeSelectedIndexChanged()
        {

        }
        protected virtual void OnListViewTypeMouseUp()
        {
            //propertyGrid.Select();
        }
        public void SetPreselectListViewItem(int index)
        {
            // Used by Advisor
            _preselectIndex = index;
        }
        public void PreselectListViewItem(int index)
        {
            _preselectIndex = index;
            //
            if (_preselectIndex >= 0 && _preselectIndex < lvTypes.Items.Count)
            {
                lvTypes.Items[_preselectIndex].Selected = true;
                lvTypes.Enabled = false;
                _preselectIndex = -1;
            }
        }
    }
}

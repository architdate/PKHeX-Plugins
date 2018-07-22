using System.Drawing;
using System.Windows.Forms;

namespace pkmn_ntr.Helpers
{
    public static class Delg
    {
        #region All controls

        delegate void SetTextDelegate(Control ctrl, string text);

        public static void SetText(Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                SetTextDelegate del = new SetTextDelegate(SetText);
                ctrl.Invoke(del, ctrl, text);
            }
            else
                ctrl.Text = text;
        }

        delegate void SeVisibleDelegate(Control ctrl, bool en);

        public static void SetVisible(Control ctrl, bool en)
        {
            if (ctrl.InvokeRequired)
            {
                SeVisibleDelegate del = new SeVisibleDelegate(SetVisible);
                ctrl.Invoke(del, ctrl, en);
            }
            else
                ctrl.Visible = en;
        }

        delegate void SetEnabledDelegate(Control ctrl, bool en);

        public static void SetEnabled(Control ctrl, bool en)
        {
            if (ctrl.InvokeRequired)
            {
                SetEnabledDelegate del = new SetEnabledDelegate(SetEnabled);
                ctrl.Invoke(del, ctrl, en);
            }
            else
                ctrl.Enabled = en;
        }

        delegate void SetColorDelegate(Control ctrl, Color c, bool back);

        public static void SetColor(Control ctrl, Color c, bool back)
        {
            if (ctrl.InvokeRequired)
            {
                SetColorDelegate del = new SetColorDelegate(SetColor);
                ctrl.Invoke(del, ctrl, c, back);
            }
            else
            {
                if (back)
                    ctrl.BackColor = c;
                else
                    ctrl.ForeColor = c;
            }
        }

        #endregion All controls

        #region CheckBox

        delegate void SetCheckedDelegate(CheckBox ctrl, bool en);

        public static void SetChecked(CheckBox ctrl, bool en)
        {
            if (ctrl.InvokeRequired)
            {
                SetCheckedDelegate del = new SetCheckedDelegate(SetChecked);
                ctrl.Invoke(del, ctrl, en);
            }
            else
                ctrl.Checked = en;
        }

        #endregion Checkbox

        #region ComboBox

        delegate void SetSelectedIndexDelegate(ComboBox ctrl, int i);

        public static void SetSelectedIndex(ComboBox ctrl, int i)
        {
            if (ctrl.InvokeRequired)
            {
                SetSelectedIndexDelegate del = new SetSelectedIndexDelegate(SetSelectedIndex);
                ctrl.Invoke(del, ctrl, i);
            }
            else
                ctrl.SelectedIndex = i;
        }

        delegate void SetSelectedValueDelegate(ComboBox ctrl, int i);

        public static void SetSelectedValue(ComboBox ctrl, int i)
        {
            if (ctrl.InvokeRequired)
            {
                SetSelectedValueDelegate del = new SetSelectedValueDelegate(SetSelectedValue);
                ctrl.Invoke(del, ctrl, i);
            }
            else
                ctrl.SelectedValue = i;
        }

        delegate void ComboboxFillDelegate(ComboBox ctrl, string[] val);

        public static void ComboboxFill(ComboBox ctrl, string[] val)
        {
            if (ctrl.InvokeRequired)
            {
                ComboboxFillDelegate del = new ComboboxFillDelegate(ComboboxFill);
                ctrl.Invoke(del, ctrl, val);
            }
            else
            {
                ctrl.Items.Clear();
                if (val != null)
                {
                    ctrl.Items.AddRange(val);
                }
            }
        }

        #endregion ComboBox

        #region DataGridView

        delegate void DataGridViewAddRowDelegate(DataGridView ctrl, params object[] args);

        public static void DataGridViewAddRow(DataGridView ctrl, params object[] args)
        {
            if (ctrl.InvokeRequired)
            {
                DataGridViewAddRowDelegate del = new DataGridViewAddRowDelegate(DataGridViewAddRow);
                ctrl.Invoke(del, args);
            }
            else
            {
                ctrl.Rows.Add(args);
            }
        }

        #endregion DataGridView

        #region NumericUpDown

        delegate void SetValueDelegate(NumericUpDown ctrl, decimal val);

        public static void SetValue(NumericUpDown ctrl, decimal val)
        {
            if (ctrl.InvokeRequired)
            {
                SetValueDelegate del = new SetValueDelegate(SetValue);
                ctrl.Invoke(del, ctrl, val);
            }
            else
                ctrl.Value = val;
        }

        delegate void SetMaximumDelegate(NumericUpDown ctrl, decimal val);

        public static void SetMaximum(NumericUpDown ctrl, decimal val)
        {
            if (ctrl.InvokeRequired)
            {
                SetMaximumDelegate del = new SetMaximumDelegate(SetMaximum);
                ctrl.Invoke(del, ctrl, val);
            }
            else
                ctrl.Maximum = val;
        }

        delegate void SetMinimumDelegate(NumericUpDown ctrl, decimal val);

        public static void SetMinimum(NumericUpDown ctrl, decimal val)
        {
            if (ctrl.InvokeRequired)
            {
                SetMinimumDelegate del = new SetMinimumDelegate(SetMinimum);
                ctrl.Invoke(del, ctrl, val);
            }
            else
                ctrl.Minimum = val;
        }

        #endregion NumericUpDown

        #region ListBox



        #endregion ListBox

        #region RadioButton

        delegate void SetCheckedRadioDelegate(RadioButton ctrl, bool en);

        public static void SetCheckedRadio(RadioButton ctrl, bool en)
        {
            if (ctrl.InvokeRequired)
            {
                SetCheckedRadioDelegate del = new SetCheckedRadioDelegate(SetCheckedRadio);
                ctrl.Invoke(del, ctrl, en);
            }
            else
                ctrl.Checked = en;
        }

        #endregion RadioButton

        #region TextBox

        delegate void SetReadOnlyDelegate(TextBox ctrl, bool en);

        public static void SetReadOnly(TextBox ctrl, bool en)
        {
            if (ctrl.InvokeRequired)
            {
                SetReadOnlyDelegate del = new SetReadOnlyDelegate(SetReadOnly);
                ctrl.Invoke(del, ctrl, en);
            }
            else
                ctrl.ReadOnly = en;
        }

        #endregion TextBox

        #region ToolTip

        delegate void SetTooltipDelegate(ToolTip source, Control ctrl, string text);

        public static void SetTooltip(ToolTip source, Control ctrl, string text)
        {
            if (ctrl.InvokeRequired)
            {
                SetTooltipDelegate del = new SetTooltipDelegate(SetTooltip);
                ctrl.Invoke(del, source, ctrl, text);
            }
            else
                source.SetToolTip(ctrl, text);
        }

        delegate void RemoveTooltipDelegate(ToolTip source, Control ctrl);

        public static void RemoveTooltip(ToolTip source, Control ctrl)
        {
            if (ctrl.InvokeRequired)
            {
                RemoveTooltipDelegate del = new RemoveTooltipDelegate(RemoveTooltip);
                ctrl.Invoke(del, source, ctrl);
            }
            else
                source.RemoveAll();
        }

        #endregion Tooltip
    }
}

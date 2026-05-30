using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace AppRestarter
{
    public static class FontManager
    {
        private static float _baseFontSize = 10.0f;
        private static readonly HashSet<string> _excludedControlNames = new HashSet<string>
        {
            "btnNavApps",
            "btnNavPcs", 
            "btnNavSettings"
        };

        public static float BaseFontSize
        {
            get => _baseFontSize;
            set
            {
                // Snap to allowed values: 9, 10, 12, 14
                if (value <= 9.0f) _baseFontSize = 9.0f;
                else if (value <= 10.0f) _baseFontSize = 10.0f;
                else if (value <= 13.0f) _baseFontSize = 12.0f;
                else _baseFontSize = 14.0f;
            }
        }

        public static Font GetFont(float relativeSize = 1.0f, FontStyle style = FontStyle.Regular)
        {
            float size = _baseFontSize * relativeSize;
            return new Font("Segoe UI", size, style);
        }

        public static void ApplyFontToControl(Control control, float relativeSize = 1.0f, FontStyle style = FontStyle.Regular)
        {
            if (control == null) return;
            control.Font = GetFont(relativeSize, style);
        }

        /// <summary>
        /// Configures a form for proper font scaling with auto-sizing.
        /// Call this after InitializeComponent() in form constructors.
        /// </summary>
        public static void ConfigureFormForScaling(Form form)
        {
            if (form == null) return;

            // Enable auto-scaling based on font
            form.AutoScaleMode = AutoScaleMode.Font;
            form.Font = GetFont();

            // Allow form to grow with content
            if (form.FormBorderStyle == FormBorderStyle.FixedDialog)
            {
                form.AutoSize = true;
                form.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            }

            // Apply fonts to all controls
            ApplyFontToControlHierarchy(form);

            // Force layout update
            form.PerformLayout();
        }

        public static void ApplyFontToForm(Form form, bool applyToChildren = true)
        {
            if (form == null) return;

            form.Font = GetFont();

            if (applyToChildren)
            {
                ApplyFontToControlHierarchy(form);
            }

            // Force layout recalculation
            form.PerformLayout();
        }

        private static void ApplyFontToControlHierarchy(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                // Skip excluded controls (navigation buttons)
                if (_excludedControlNames.Contains(control.Name))
                {
                    if (control.HasChildren)
                    {
                        ApplyFontToControlHierarchy(control);
                    }
                    continue;
                }

                // Apply font to interactive controls
                if (control is Button || control is Label || control is CheckBox || 
                    control is TextBox || control is ComboBox || control is ListBox ||
                    control is DataGridView || control is NumericUpDown || control is GroupBox ||
                    control is CheckedListBox)
                {
                    control.Font = GetFont();
                }

                // Recurse into child controls
                if (control.HasChildren)
                {
                    ApplyFontToControlHierarchy(control);
                }
            }
        }

        public static void ApplyScaledFontToForm(Form form)
        {
            if (form == null) return;
            ApplyFontToForm(form, true);
        }
    }
}

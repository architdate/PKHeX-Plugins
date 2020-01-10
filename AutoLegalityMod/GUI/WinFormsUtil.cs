using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using System.Drawing;

namespace AutoModPlugins
{
    public static class WinFormsUtil
    {
        public static DialogResult Alert(params string[] lines)
        {
            SystemSounds.Asterisk.Play();
            string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            return MessageBox.Show(msg, nameof(Alert), MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static DialogResult Prompt(MessageBoxButtons btn, params string[] lines)
        {
            SystemSounds.Question.Play();
            string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            return MessageBox.Show(msg, nameof(Prompt), btn, MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// Displays a dialog showing the details of an error.
        /// </summary>
        /// <param name="lines">User-friendly message about the error.</param>
        /// <returns>The <see cref="DialogResult"/> associated with the dialog.</returns>
        public static DialogResult Error(params string[] lines)
        {
            SystemSounds.Hand.Play();
            string msg = string.Join(Environment.NewLine + Environment.NewLine, lines);
            return MessageBox.Show(msg, nameof(Error), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Opens a dialog to open a PKM/SAV file.
        /// </summary>
        /// <param name="Extensions">Misc extensions of files supported.</param>
        /// <param name="path">Output result path</param>
        /// <returns>Result of whether or not a file is to be loaded from the output path.</returns>
        public static bool OpenSAVPKMDialog(IEnumerable<string> Extensions, out string path)
        {
            string supported = string.Join(";", Extensions.Select(s => $"*.{s}").Concat(new[] { "*.pkm" }));
            using var ofd = new OpenFileDialog
            {
                Filter = "All Files|*.*" +
                         $"|Supported Files (*.*)|main;*.bin;{supported};*.bak" +
                         "|Save Files (*.sav)|main" +
                         "|Decrypted PKM File (*.pkm)|" + supported +
                         "|Binary File|*.bin" +
                         "|Backup File|*.bak"
            };
            if (ofd.ShowDialog() != DialogResult.OK)
            {
                path = null;
                return false;
            }

            path = ofd.FileName;
            return true;
        }

        internal static void CenterToForm(this Control child, Control parent)
        {
            int x = parent.Location.X + ((parent.Width - child.Width) / 2);
            int y = parent.Location.Y + ((parent.Height - child.Height) / 2);
            child.Location = new Point(Math.Max(x, 0), Math.Max(y, 0));
        }
    }
}
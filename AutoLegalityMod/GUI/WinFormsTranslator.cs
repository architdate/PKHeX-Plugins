﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AutoModPlugins.Properties;
using PKHeX.Core;

namespace AutoModPlugins
{
    // Code borrowed from PKHeX.WinForms with permission from kwsch, with adaptations
    public static class WinFormsTranslator
    {
        private static readonly Dictionary<string, TranslationContext> Context = [];

        internal static void TranslateInterface(this Control form, string lang) =>
            TranslateForm(form, GetContext(lang));

        private static string GetTranslationFileNameInternal(string lang) => $"almlang_{lang}";

        private static string GetTranslationFileNameExternal(string lang) => $"almlang_{lang}.txt";

        public static string CurrentLanguage
        {
            get => GameInfo.CurrentLanguage;
            private set => GameInfo.CurrentLanguage = value;
        }

        internal static TranslationContext GetContext(string lang)
        {
            if (Context.TryGetValue(lang, out var context))
                return context;

            var lines = GetTranslationFile(lang);
            Context.Add(lang, context = new TranslationContext(lines));
            return context;
        }

        private static void TranslateForm(Control form, TranslationContext context)
        {
            form.SuspendLayout();
            var formname = form.Name;

            // Translate Title
            form.Text = context.GetTranslatedText(formname, form.Text);
            var translatable = GetTranslatableControls(form);
            foreach (var c in translatable)
            {
                if (c is Control r)
                {
                    var current = r.Text;
                    var updated = context.GetTranslatedText($"{formname}.{r.Name}", current);
                    if (!ReferenceEquals(current, updated))
                        r.Text = updated;
                }
                else if (c is ToolStripItem t)
                {
                    var current = t.Text;
                    var updated = context.GetTranslatedText($"{formname}.{t.Name}", current);
                    if (!ReferenceEquals(current, updated))
                        t.Text = updated;
                }
            }

            form.ResumeLayout();
        }

        private static IEnumerable<string> GetTranslationFile(string lang)
        {
            var file = GetTranslationFileNameInternal(lang);

            // Check to see if the translation file exists in the same folder as the executable
            string externalLangPath = GetTranslationFileNameExternal(file);
            if (File.Exists(externalLangPath))
            {
                try
                {
                    return File.ReadAllLines(externalLangPath);
                }
                catch
                { /* In use? Just return the internal resource. */
                }
            }

            if (Util.IsStringListCached(file, out var result))
                return result;
            var txt = Resources.ResourceManager.GetObject(file);
            if (txt is not string s)
                return Array.Empty<string>();
            return Util.LoadStringList(file, s);
        }

        private static IEnumerable<object> GetTranslatableControls(Control f)
        {
            foreach (var z in f.GetChildrenOfType<Control>())
            {
                switch (z)
                {
                    case ToolStrip menu:
                        foreach (var obj in GetToolStripMenuItems(menu))
                            yield return obj;

                        break;
                    default:
                        if (string.IsNullOrWhiteSpace(z.Name))
                            break;

                        if (z.ContextMenuStrip != null)
                        {
                            foreach (var obj in GetToolStripMenuItems(z.ContextMenuStrip))
                                yield return obj;
                        }

                        if (z is ListControl or TextBoxBase or LinkLabel or NumericUpDown or ContainerControl)
                            break; // undesirable to modify, ignore

                        if (!string.IsNullOrWhiteSpace(z.Text))
                            yield return z;
                        break;
                }
            }
        }

        private static IEnumerable<T> GetChildrenOfType<T>(this Control control)
            where T : class
        {
            foreach (Control child in control.Controls)
            {
                if (child is T childOfT)
                    yield return childOfT;

                if (!child.HasChildren)
                    continue;
                foreach (var descendant in GetChildrenOfType<T>(child))
                    yield return descendant;
            }
        }

        private static IEnumerable<object> GetToolStripMenuItems(ToolStrip menu)
        {
            foreach (var i in menu.Items.OfType<ToolStripMenuItem>())
            {
                if (!string.IsNullOrWhiteSpace(i.Text))
                    yield return i;
                foreach (var sub in GetToolsStripDropDownItems(i).Where(z => !string.IsNullOrWhiteSpace(z.Text)))
                    yield return sub;
            }
        }

        private static IEnumerable<ToolStripMenuItem> GetToolsStripDropDownItems(
            ToolStripDropDownItem item
        )
        {
            foreach (var dropDownItem in item.DropDownItems.OfType<ToolStripMenuItem>())
            {
                yield return dropDownItem;
                if (!dropDownItem.HasDropDownItems)
                    continue;
                foreach (ToolStripMenuItem subItem in GetToolsStripDropDownItems(dropDownItem))
                    yield return subItem;
            }
        }

        public static void UpdateAll(string baseLanguage, IEnumerable<string> others)
        {
            var basecontext = GetContext(baseLanguage);
            foreach (var lang in others)
            {
                var c = GetContext(lang);
                c.UpdateFrom(basecontext);
            }
        }

        public static void DumpAll(params string[] banlist)
        {
            var results = Context.Select(z => new { Lang = z.Key, Lines = z.Value.Write() });
            foreach (var c in results)
            {
                var lang = c.Lang;
                var fn = GetTranslationFileNameExternal(lang);
                var lines = c.Lines;
                var result = lines.Where(z => !banlist.Any(z.Contains));
                File.WriteAllLines(fn, result);
            }
        }

        public static void LoadAllForms(params string[] banlist)
        {
            var q = Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.BaseType == typeof(Form) && !banlist.Contains(t.Name));
            foreach (var t in q)
            {
                var constructors = t.GetConstructors();
                if (constructors.Length == 0)
                {
                    Console.WriteLine($"No constructors: {t.Name}");
                    continue;
                }

                var argCount = constructors[0].GetParameters().Length;
                try
                {
                    var _ = (Form)(
                        Activator.CreateInstance(t, new object[argCount])
                        ?? throw new Exception("Null Activator instance")
                    );
                }
                catch
                {
                    // Discard any exception, we're just creating temp forms.
                }
            }
        }

        public static void SetRemovalMode(bool status = true)
        {
            foreach (TranslationContext c in Context.Values)
            {
                c.RemoveUsedKeys = status;
                c.AddNew = !status;
            }
        }

        public static void RemoveAll(string defaultLanguage, params string[] banlist)
        {
            var badKeys = Context[defaultLanguage];
            var split = badKeys
                .Write()
                .Select(z => z.Split(TranslationContext.Separator)[0])
                .Where(l => !banlist.Any(l.StartsWith))
                .ToArray();
            foreach (var c in Context)
            {
                var lang = c.Key;
                var fn = GetTranslationFileNameExternal(lang);
                var lines = File.ReadAllLines(fn);
                var result = lines.Where(
                    l => !split.Any(s => l.StartsWith(s + TranslationContext.Separator))
                );
                File.WriteAllLines(fn, result);
            }
        }
    }

    public sealed class TranslationContext
    {
        public bool AddNew { private get; set; }
        public bool RemoveUsedKeys { private get; set; }
        public const char Separator = '=';
        private readonly Dictionary<string, string> translation = [];

        public TranslationContext(IEnumerable<string> content, char separator = Separator)
        {
            var entries = content.Select(z => z.Split(separator)).Where(z => z.Length == 2);
            foreach (var kvp in entries.Where(z => !translation.ContainsKey(z[0])))
                translation.Add(kvp[0], kvp[1]);
        }

        public string? GetTranslatedText(string val, string? fallback)
        {
            if (RemoveUsedKeys)
                translation.Remove(val);

            if (translation.TryGetValue(val, out var translated))
                return translated;

            if (fallback != null && AddNew)
                translation.Add(val, fallback);
            return fallback;
        }

        public IEnumerable<string> Write(char separator = Separator)
        {
            return translation
                .Select(z => $"{z.Key}{separator}{z.Value}")
                .OrderBy(z => z.Contains('.'))
                .ThenBy(z => z);
        }

        public void UpdateFrom(TranslationContext other)
        {
            bool oldAdd = AddNew;
            AddNew = true;
            foreach (var kvp in other.translation)
                GetTranslatedText(kvp.Key, kvp.Value);
            AddNew = oldAdd;
        }

        public void RemoveKeys(TranslationContext other)
        {
            foreach (var kvp in other.translation)
                translation.Remove(kvp.Key);
        }
    }
}

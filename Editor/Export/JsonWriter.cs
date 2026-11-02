// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ADVOSC.Triggers.Editor {
    /// <summary>
    /// A small JSON writer. JsonUtility can't write the bundle's mixed action list, and adding a
    /// JSON package isn't worth it for one format. Numbers always use the invariant culture.
    /// </summary>
    internal sealed class JsonWriter {
        private readonly StringBuilder output = new StringBuilder();
        private readonly Stack<bool> hasItems = new Stack<bool>();
        private readonly bool pretty;
        private bool afterKey;

        /// <summary>Every string written, keys included, so the exporter can check them all.</summary>
        public readonly List<string> Strings = new List<string>();

        public JsonWriter(bool pretty) {
            this.pretty = pretty;
        }

        public override string ToString() {
            return output.ToString();
        }

        public void BeginObject() {
            Open('{');
        }

        public void EndObject() {
            Close('}');
        }

        public void BeginArray() {
            Open('[');
        }

        public void EndArray() {
            Close(']');
        }

        public void Key(string name) {
            BeforeValue();
            WriteString(name);
            output.Append(pretty ? ": " : ":");
            afterKey = true;
        }

        public void Value(string value) {
            BeforeValue();
            WriteString(value);
        }

        public void Value(bool value) {
            BeforeValue();
            output.Append(value ? "true" : "false");
        }

        public void Value(int value) {
            BeforeValue();
            output.Append(value.ToString(CultureInfo.InvariantCulture));
        }

        public void Value(float value) {
            BeforeValue();
            output.Append(((double)value).ToString("0.######", CultureInfo.InvariantCulture));
        }

        private void Open(char bracket) {
            BeforeValue();
            output.Append(bracket);
            hasItems.Push(false);
        }

        private void Close(char bracket) {
            var hadItems = hasItems.Pop();
            if (hadItems) NewLine();
            output.Append(bracket);
        }

        private void BeforeValue() {
            if (afterKey) {
                afterKey = false;
                return;
            }
            if (hasItems.Count == 0) return;
            if (hasItems.Pop()) output.Append(',');
            hasItems.Push(true);
            NewLine();
        }

        private void NewLine() {
            if (!pretty) return;
            output.Append('\n');
            output.Append(' ', hasItems.Count * 2);
        }

        private void WriteString(string value) {
            Strings.Add(value);
            output.Append('"');
            foreach (var c in value) {
                switch (c) {
                    case '"':
                        output.Append("\\\"");
                        break;
                    case '\\':
                        output.Append("\\\\");
                        break;
                    case '\n':
                        output.Append("\\n");
                        break;
                    case '\r':
                        output.Append("\\r");
                        break;
                    case '\t':
                        output.Append("\\t");
                        break;
                    case '\b':
                        output.Append("\\b");
                        break;
                    case '\f':
                        output.Append("\\f");
                        break;
                    default:
                        if (c < 0x20) {
                            output.Append("\\u");
                            output.Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        } else {
                            output.Append(c);
                        }
                        break;
                }
            }
            output.Append('"');
        }
    }
}

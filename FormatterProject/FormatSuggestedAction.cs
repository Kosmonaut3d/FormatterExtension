using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace TestLightBulb
{
    internal class FormatSuggestedAction : ISuggestedAction
    {
        private readonly ITextSnapshot _snapshot;
        private readonly string _upper;
        private readonly string _display;
        private SnapshotSpan _range;
        
        private int _startingLine;

        private LineOutputStruct[] finalOutput;

        private struct LineInfoStruct
        {
            public int dLine;
            public int[] separatorSequence;
            public int[] separatorIndices;
            public int dIndent;
            public string[] stringBlocks;
        }

        private struct LineOutputStruct
        {
            public int dLine;
            public string sOutput;
        }

        public FormatSuggestedAction(ITextSnapshot snapshot, int startingLine, SnapshotSpan range)
        {
            _range = range;
            _snapshot = snapshot;
            _startingLine = startingLine;
            int lineAmount = Calculate();
            _display = string.Format("Autoformat these {0} lines", lineAmount);
        }
        
        private int Calculate()
        {
            List<LineInfoStruct> formatLines = new List<LineInfoStruct>();

            LineInfoStruct currentLine = FillStruct(_startingLine);
            formatLines.Add(currentLine);

            int lineIndex = _startingLine;

            //next lines
            while(true)
            {
                LineInfoStruct nextLine = FillStruct(++lineIndex);

                if(GetArrayEqual(currentLine.separatorSequence, nextLine.separatorSequence))
                {
                    formatLines.Add(nextLine);
                }
                else
                {
                    break;
                }
            }

            lineIndex = _startingLine;
            //prev lines
            while (true)
            {
                LineInfoStruct nextLine = FillStruct(--lineIndex);

                if (GetArrayEqual(currentLine.separatorSequence, nextLine.separatorSequence))
                {
                    formatLines.Add(nextLine);
                }
                else
                {
                    break;
                }
            }

            //Find minimum indent
            int dMinimumIndent = 0;
            dMinimumIndent = formatLines.Min(s => s.dIndent);
            string sIndent = new string(' ', dMinimumIndent);

            int indexCount = currentLine.separatorIndices.Length;
            int[] maxSeparatorsIndices = new int[indexCount];

            for(int i = 0; i< indexCount; i++)
            {
                maxSeparatorsIndices[i] = formatLines.Max(s => s.separatorIndices[i]);
            }

            int lineCount = formatLines.Count;

            finalOutput = new LineOutputStruct[lineCount];
            //Generate new text
            for (int k = 0; k < lineCount; k++)
            {
                finalOutput[k].dLine = formatLines[k].dLine;
                finalOutput[k].sOutput = sIndent;

                for(int index = 0; index < indexCount; index++)
                {
                    finalOutput[k].sOutput += formatLines[k].stringBlocks[index];
                    finalOutput[k].sOutput += new string(' ', maxSeparatorsIndices[index] + (index+1) - finalOutput[k].sOutput.Length);
                    finalOutput[k].sOutput += " "+(Convert.ToChar(formatLines[k].separatorSequence[index]));

                }
            }
            
            return lineCount;
        }

        private LineInfoStruct FillStruct(int line)
        {
            LineInfoStruct output = new LineInfoStruct();
            output.dLine = line;
            string text;
            GetSeparatorSequence(line, out output.separatorSequence, out output.separatorIndices, out text);

            if (output.separatorSequence.Length > 0)
            {
                int dIndentLevel = 0;
                while (text[dIndentLevel] == ' ')
                {
                    dIndentLevel++;
                };
                output.dIndent = dIndentLevel;
            }

            text = text.Replace(" ", string.Empty);
            output.stringBlocks = text.Split('=', ';', ',', '(', ')', '{', '}', '[', ']');
            return output;
        }
        
        private bool GetSeparatorSequence(int dLine, out int[] outSeparatorSequence, out int[] outSeparatorIndices, out string outText)
        {
            outSeparatorIndices = new int[0];
            outSeparatorSequence = new int[0];
            outText = "";
            if (dLine < 0) return false;

            ITextSnapshotLine snapLine = _snapshot.GetLineFromLineNumber(dLine);

            if (snapLine == null) return false;

            outText = snapLine.GetText();

            int dNumberOfSeparators = 0;

            for (int i = 0; i < outText.Length; i++)
            {
                char test = outText[i];
                if (test == '=' || test == ';' || test == ',' || test == '(' || test == ')' || test == '{' || test == '}' || test == '[' || test == ']')
                {
                    dNumberOfSeparators++;
                }
            }

            outSeparatorSequence = new int[dNumberOfSeparators];
            outSeparatorIndices = new int[dNumberOfSeparators];

            int j = 0;

            for (int i = 0; i < outText.Length; i++)
            {
                char test = outText[i];
                if (test == '=' || test == ';' || test == ',' || test == '(' || test == ')' || test == '{' || test == '}' || test == '[' || test == ']')
                {
                    outSeparatorSequence[j] = (int)test;
                    outSeparatorIndices[j] = i;
                    j++;
                }
            }

            return false;
        }

        private bool GetArrayEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        public string DisplayText
        {
            get
            {
                return _display;
            }
        }

        public string IconAutomationText
        {
            get
            {
                return null;
            }
        }

        ImageMoniker ISuggestedAction.IconMoniker
        {
            get
            {
                return default(ImageMoniker);
            }
        }

        public string InputGestureText
        {
            get
            {
                return null;
            }
        }

        public bool HasActionSets
        {
            get
            {
                return false;
            }
        }

        public Task<IEnumerable<SuggestedActionSet>> GetActionSetsAsync(CancellationToken cancellationToken)
        {
            return null;
        }

        public bool HasPreview
        {
            get
            {
                return false;
            }
        }

        public Task<object> GetPreviewAsync(CancellationToken cancellationToken)
        {
            var textBlock = new TextBlock();
            textBlock.Padding = new Thickness(5);
            textBlock.Inlines.Add(new Run() { Text = _upper });
            return Task.FromResult<object>(textBlock);
        }

        public void Dispose()
        {
        }

        public void Invoke(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            ITextEdit edit = _snapshot.TextBuffer.CreateEdit();

            foreach (LineOutputStruct strc in finalOutput)
            {
                ITextSnapshotLine snapLine = _snapshot.GetLineFromLineNumber(strc.dLine);
                SnapshotSpan span = new SnapshotSpan(snapLine.Snapshot, snapLine.Start, snapLine.Length);

                edit.Replace(span, strc.sOutput);

            }

            ///ITextSnapshotLine snapLine = _snapshot.GetLineFromLineNumber(_startingLine);

            //SnapshotSpan span = new SnapshotSpan(snapLine.Snapshot, snapLine.Start, snapLine.Length);
            //edit.Replace(_range, "test");

            edit.Apply();
            //_span.TextBuffer.Replace(_span.GetSpan(_snapshot), _upper);
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

    }
}

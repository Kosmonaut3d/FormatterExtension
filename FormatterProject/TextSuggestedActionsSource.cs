using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestLightBulb
{
    class TestSuggestedActionsSource : ISuggestedActionsSource
    {
        private readonly TestSuggestedActionsSourceProvider _factory;
        private readonly ITextBuffer _textBuffer;
        private readonly ITextView _textView;

        private enum Seperators
        {
            BracketOpen,
            BracketClose,
            FancyBracketOpen,
            FancyBracketClose,
            Comma,
            Semicolon,
            Equal
        }
        
        public TestSuggestedActionsSource(TestSuggestedActionsSourceProvider testSuggestedActionsSourceProvider, ITextView textView, ITextBuffer textBuffer)
        {
            _factory = testSuggestedActionsSourceProvider;
            _textBuffer = textBuffer;
            _textView = textView;
        }

#pragma warning disable 0067
        public event EventHandler<EventArgs> SuggestedActionsChanged;
#pragma warning restore 0067

        public void Dispose()
        {
        }

        public IEnumerable<SuggestedActionSet> GetSuggestedActions(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            //TextExtent extent;
            //int test = TryGetLineCountUnderCaret(range);
            if (TryFindPattern(range))
            {
                int currentLine = _textBuffer.CurrentSnapshot.GetLineNumberFromPosition(range.Start);
                var formatAction = new FormatSuggestedAction(_textBuffer.CurrentSnapshot, currentLine, range);
                return new SuggestedActionSet[] { new SuggestedActionSet(new ISuggestedAction[] { formatAction }) };
            }
            return Enumerable.Empty<SuggestedActionSet>();
        }

        public Task<bool> HasSuggestedActionsAsync(ISuggestedActionCategorySet requestedActionCategories, SnapshotSpan range, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                return TryFindPattern(range);
            });
        }

        private bool TryFindPattern(SnapshotSpan range)
        {
            if (_textView.Caret.ContainingTextViewLine.Length < 4) return false;

            ITextSnapshot _snapshot = _textBuffer.CurrentSnapshot;

            int currentLine = _snapshot.GetLineNumberFromPosition(range.Start);

            string sPrototype = _snapshot.GetLineFromLineNumber(currentLine).GetText();

            //int dIndentLevel = 0;

            //while (sPrototype[dIndentLevel] == ' ')
            //{
            //    dIndentLevel++;
            //};

            //sPrototype = sPrototype.Remove(0, dIndentLevel);
            
            int[] blockSequenceMainLine = GetBlockSequence(currentLine, _snapshot);
            
            //Check next and previous lines
            int[] blockSeqNextLine = GetBlockSequence(currentLine + 1, _snapshot);
            if (GetArrayEqual(blockSequenceMainLine, blockSeqNextLine))
            {
                return true;
            }

            int[] blockSeqPrevLine = GetBlockSequence(currentLine - 1, _snapshot);
            if (GetArrayEqual(blockSequenceMainLine, blockSeqPrevLine))
            {
                return true;
            }

            return false;
        }

        private bool GetArrayEqual(int[] a, int[] b)
        {
            if (a.Length != b.Length) return false;

            for(int i = 0; i<a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        private int GetNumberOfBlocks(string sInput)
        {
            return sInput.Split(';', '(', ')', '{', '}', ',', '=').Length;
        }

        private int[] GetBlockSequence(int dLine, ITextSnapshot snapshot)
        {
            if (dLine < 0) return new int[0];

            ITextSnapshotLine snapLine = snapshot.GetLineFromLineNumber(dLine);

            if(snapLine == null) return new int[0];

            string sInput = snapLine.GetText();

            int dNumberOfSeparators = 0;

            for (int i = 0; i < sInput.Length; i++)
            {
                char test = sInput[i];
                if (test == '=' || test == ';' || test == ',' || test == '(' || test == ')' || test == '{' || test == '}' || test == '[' || test == ']')
                {
                    dNumberOfSeparators++;
                }
            }

            int[] blockSequence = new int[dNumberOfSeparators];

            int j = 0;

            for (int i = 0; i < sInput.Length; i++)
            {
                char test = sInput[i];
                if (test == '=' || test == ';' || test == ',' || test == '(' || test == ')' || test == '{' || test == '}' || test == '[' || test == ']')
                {
                    blockSequence[j] = (int)test;
                    j++;
                }
            }
            return blockSequence;
        }

        public bool TryGetTelemetryId(out Guid telemetryId)
        {
            telemetryId = Guid.Empty;
            return false;
        }

        private bool TryGetWordUnderCaret(out TextExtent wordExtent)
        {
            ITextCaret caret = _textView.Caret;
            SnapshotPoint point;

            if (caret.Position.BufferPosition > 0)
            {
                point = caret.Position.BufferPosition - 1;
            }
            else
            {
                wordExtent = default(TextExtent);
                return false;
            }

            ITextStructureNavigator navigator = _factory.NavigatorService.GetTextStructureNavigator(_textBuffer);

            wordExtent = navigator.GetExtentOfWord(point);
            return true;
        }
    }
}

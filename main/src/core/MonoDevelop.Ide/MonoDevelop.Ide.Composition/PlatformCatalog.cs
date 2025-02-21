//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Composition;

namespace Microsoft.VisualStudio.Platform
{
    [Export]
	// TODO: editor obsolete
    internal class PlatformCatalog
    {
		static PlatformCatalog instance;
		public static PlatformCatalog Instance {
			get {
				if (instance == null) {
					instance = CompositionManager.Instance.GetExportedValue<PlatformCatalog> ();
				}

				return instance;
			}
		}

        [Import]
        internal ITextBufferFactoryService _textBufferFactoryService { get; private set; }

		public ITextBufferFactoryService3 TextBufferFactoryService => (ITextBufferFactoryService3)_textBufferFactoryService;

		[Import]
        internal ITextDocumentFactoryService TextDocumentFactoryService { get; private set; }

	// oe REVERTED...
	[Import]
	internal ITextEditorFactoryService TextEditorFactoryService { get; private set; }

	// oe REVERTED...
        [Import]
        internal IMimeToContentTypeRegistryService MimeToContentTypeRegistryService { get; private set; }

        [Import]
        internal IContentTypeRegistryService ContentTypeRegistryService { get; private set; }

        [Import]
        internal IBufferTagAggregatorFactoryService BufferTagAggregatorFactoryService { get; private set; }

		[Import]
		internal IClassifierAggregatorService ClassifierAggregatorService { get; private set; }

		[Import]
		internal IViewClassifierAggregatorService ViewClassifierAggregatorService { get; private set; }
    }

	// oe REVERTED...
    public interface IMimeToContentTypeRegistryService
    {
        string GetMimeType(IContentType type);
        IContentType GetContentType(string type);

        void LinkTypes(string mimeType, IContentType contentType);
    }

	// oe REVERTED...
    [Export(typeof(IMimeToContentTypeRegistryService))]
    public class MimeToContentTypeRegistryService : IMimeToContentTypeRegistryService, IPartImportsSatisfiedNotification
    {
		[Import]
		IContentTypeRegistryService ContentTypeRegistryService { get; set; }

		[Export]
		[Name ("csharp")]
		[BaseDefinition ("code")]
		public ContentTypeDefinition codeContentType;

		public string GetMimeType(IContentType type)
        {
            string mimeType;
            if (this.maps.Item2.TryGetValue(type, out mimeType))
            {
                return mimeType;
            }

            return (ContentTypeRegistryService as IContentTypeRegistryService2).GetMimeType (type);
        }

        public IContentType GetContentType(string type)
        {
            IContentType contentType;
            if (this.maps.Item1.TryGetValue(type, out contentType))
            {
                return contentType;
            }

            return (ContentTypeRegistryService as IContentTypeRegistryService2).GetContentTypeForMimeType (type);
        }

        public void LinkTypes(string mimeType, IContentType contentType)
        {
            var oldMap = Volatile.Read(ref this.maps);
            while (true)
            {
                if (oldMap.Item1.ContainsKey(mimeType) || oldMap.Item2.ContainsKey(contentType))
                    break;

                var newMap = Tuple.Create(oldMap.Item1.Add(mimeType, contentType), oldMap.Item2.Add(contentType, mimeType));
                var result = Interlocked.CompareExchange(ref this.maps, newMap, oldMap);
                if (result == oldMap)
                {
                    break;
                }

                oldMap = result;
            }
        }

		void LinkTypes (string mimeType, string contentType)
		{
			LinkTypes (mimeType, ContentTypeRegistryService.GetContentType (contentType));
		}

		void IPartImportsSatisfiedNotification.OnImportsSatisfied ()
		{
			LinkTypes ("text/plain", "text");
			LinkTypes ("text/x-csharp", "csharp");
		}

		Tuple<ImmutableDictionary<string, IContentType>, ImmutableDictionary<IContentType, string>> maps = Tuple.Create(ImmutableDictionary<string, IContentType>.Empty, ImmutableDictionary<IContentType, string>.Empty);
    }

    // Fold back into Text.Def.TextData.TextSnapshotToTextReader
	// TODO: editor obsolete
    internal sealed class NewTextSnapshotToTextReader : TextReader
    {
        #region TextReader methods
        /// <summary>
        /// Closes the reader and releases any associated system resources.
        /// </summary>
        public override void Close()
        {
            _currentPosition = -1;
            base.Close();
        }

        /// <summary>
        /// Releases all resources used by the reader.
        /// </summary>
        /// <param name="disposing">Whether to release managed resources.</param>
        protected override void Dispose(bool disposing)
        {
            _currentPosition = -1;
            base.Dispose(disposing);
        }

        /// <summary>
        /// Returns the next character without changing the state of the reader or the
        /// character source.
        /// </summary>
        /// <returns>The next character to be read, or -1 if no more characters are available or the stream does not support seeking.</returns>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Peek()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            return (_currentPosition == _end) ? -1 : (int)(_snapshot[_currentPosition]);
        }

        /// <summary>
        /// Reads the next character from the input stream and advances the character
        /// position by one character.
        /// </summary>
        /// <returns>The next character from the input stream, or -1 if no more characters are available.</returns>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Read()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            return (_currentPosition == _end) ? -1 : (int)(_snapshot[_currentPosition++]);
        }

        /// <summary>
        /// Reads the specified number of characters from the current stream and writes the
        /// data to the buffer, beginning at the specified location.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>The number of characters that have been read. The number will be less than
        /// or equal to <paramref name="count"/>, depending on whether the data is available within the
        /// stream. This method returns zero if called when no more characters are left to read.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative, or
        /// the buffer length minus index is less than <paramref name="count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int Read(char[] buffer, int index, int count)
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (((index + count) < 0) || ((index + count) > buffer.Length))
                throw new ArgumentOutOfRangeException(nameof(count));

            int charactersToRead = System.Math.Min(_end - _currentPosition, count);
            _snapshot.CopyTo(_currentPosition, buffer, index, charactersToRead);
            _currentPosition += charactersToRead;

            return charactersToRead;
        }

        /// <summary>
        /// Reads a maximum of <paramref name="count"/> characters from the current stream and writes the
        /// data to buffer, beginning at index.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array from the current source.</param>
        /// <param name="index">The place in buffer at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>The number of characters that have been read. The number will be less than
        /// or equal to <paramref name="count"/>, depending on whether the data is available within the
        /// stream. This method returns zero if called when no more characters are left to read.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> or <paramref name="count"/> is negative, or
        /// the buffer length minus index is less than <paramref name="count"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ObjectDisposedException">The reader is closed.</exception>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            return Read(buffer, index, count);
        }

        /// <summary>Reads a line of characters from the current stream and returns the data as a string.</summary>
        /// <returns>The next line from the input stream, or null if all characters have been read.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed.</exception>
        public override string ReadLine()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            if (_readLastLine)
                return null;

            ITextSnapshotLine line = _snapshot.GetLineFromPosition(_currentPosition);

            //Handle the case where the current position is between a \r\n without crashing (but returning an empty string instead).
            string text = (line.End > _currentPosition)
                          ? _snapshot.GetText(_currentPosition, line.End - _currentPosition)
                          : string.Empty;

            _currentPosition = line.EndIncludingLineBreak;

            if (_currentPosition == _end)
            {
                //Do not read the last line in the buffer unless it contains some text.
                _readLastLine = true;
            }

            return text;
        }

        /// <summary>Reads all the characters from the current position to the end of the reader and returns them as a string.</summary>
        /// <returns>A string containing all the characters from the current position to the end of the reader.</returns>
        /// <exception cref="ObjectDisposedException">The <see cref="TextReader"/> is closed.</exception>
        public override string ReadToEnd()
        {
            if (_currentPosition == -1)
                throw new ObjectDisposedException("TextSnapshotToTextReader");

            string text = _snapshot.GetText(_currentPosition, _end - _currentPosition);
            _currentPosition = _end;

            return text;
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="TextSnapshotToTextReader"/> with the specified text snapshot.
        /// </summary>
        /// <param name="textSnapshot">The <see cref="ITextSnapshot"/> to expose as a reader.</param>
        /// <exception cref="ArgumentNullException"><paramref name="textSnapshot"/> is null.</exception>
        public NewTextSnapshotToTextReader(ITextSnapshot textSnapshot)
        {
            if (textSnapshot == null)
                throw new ArgumentNullException(nameof(textSnapshot));

            _snapshot = textSnapshot;
            _end = textSnapshot.Length;
        }

        public NewTextSnapshotToTextReader(ITextSnapshot textSnapshot, int offset, int length)
        {
            if (textSnapshot == null)
                throw new ArgumentNullException(nameof(textSnapshot));
            if ((offset < 0) || (offset > textSnapshot.Length))
                throw new ArgumentOutOfRangeException(nameof(offset));
            int end = offset + length;
            if ((end < offset) || (end > textSnapshot.Length))
                throw new ArgumentOutOfRangeException(nameof(length));

            _snapshot = textSnapshot;
            _currentPosition = offset;
            _end = offset + length;
        }

        ITextSnapshot _snapshot;
        int _currentPosition;
        bool _readLastLine;

        int _end;
    }
}

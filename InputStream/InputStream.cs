using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bakera.RedFace{


	public class InputStream : ParserEventSender, IDisposable{

		private StringBuilder myConsumedChars = new StringBuilder(); // Consumeされた文字の履歴
		private int myOffset = 0;
		private Stream myStream = null;
		private TextReader myTextReader = null;

		private const int SniffEncodingBufferSize = 1024;

// コンストラクタ
		public InputStream(Stream s){
			myStream = s;
		}


// プロパティ
		public EncodingConfidence EncodingConfidence{get; set;}
		public Encoding Encoding{get; set;}

		public char? CurrentInputChar{
			get {
				char? result = GetCharByPosition(CurrentPosition);
				return result;
			}
		}

		public char? NextInputChar{
			get {
				if(Offset > 0) return GetCharByPosition(CurrentPosition + 1);
				if(Offset == 0) {
					if(ConsumeNextInputChar()){
						myOffset--;
						return GetCharByPosition(CurrentPosition + 1);
					}
				}
				return null;
			}
		}

		// UnConsumeされた文字があるとき、オフセットを示す。EOFに達すると-1
		public int Offset{
			get {return myOffset;}
		}

		public int CurrentPosition{
			get{
				return myConsumedChars.Length - myOffset;
			}
		}



// メソッド
		public void Dispose(){
			if(myTextReader != null) myTextReader.Dispose();
		}


		// 1文字読みます。
		// 終端に達していたらfalseを返します。
		public bool ConsumeNextInputChar(){
			if(myOffset < 0) return false;
			if(myOffset > 0){
				myOffset--;
				return true;
			}
			bool result = ReadCharFromStream();
			if(!result) myOffset = -1;
			return result;
		}

		// Unconsumeします。
		public void UnConsume(int offset){
			myOffset += offset;
		}


// private メソッド

		// ストリームから文字を読み取ってバッファに追加します。
		// 終端に達していたらfalseを返します。
		private bool ReadCharFromStream(){
			for(;;){
				int charNum = myTextReader.Read();
				if(charNum < 0){
					return false;
				}

				// ZWNBSは無視する (willful violation)
				// BOMはTextReaderによって既に無視されているはず
				if(charNum == Chars.BOM){
					OnMessageRaised(EventLevel.Information, string.Format("文中に U+FEFF (BYTE ORDER MARK / ZERO WIDTH NO BREAK SPACE) を検出しましたが、無視します。"));
					continue;
				}

				// noncharactersはパースエラー
				// HTML5 spec ではエラー後の処理が未定義だがとりあえず無視する (バッファに取り込まない)
				if(Chars.IsErrorChar(charNum)){
					OnParseErrorRaised(string.Format("非Unicode文字 (noncharacters) が含まれています。: {0}", charNum));
					continue;
				}

				if(charNum == Chars.CARRIAGE_RETURN){
					// CR+LFの場合、LFのみバッファに入れる
					// CR+終端の場合、LFのみバッファに入れる
					// CR+何かの場合、LF+何かをバッファに入れてOffsetを+1する
					myConsumedChars.Append(Chars.LINE_FEED);
					int nextCharNum = myTextReader.Read();
					if(nextCharNum < 0 || nextCharNum == Chars.LINE_FEED) return true;
					myConsumedChars.Append((char)nextCharNum);
					myOffset++;
					return true;
				}
				myConsumedChars.Append((char)charNum);
				return true;
			}
		}

		private char? GetCharByPosition(int position){
			int index = position - 1;
			if(index < 0) return null;
			if(index >= myConsumedChars.Length) return null;
			return myConsumedChars[index];
		}



// エンコード
		// Encoding と EncodingConfidence をセットし、textReaderを初期化します。
		public void SetEncoding(Encoding enc, EncodingConfidence conf){
			if(myTextReader != null){
				//ToDo:
				throw new Exception("Encodingをあとから変更することはできません。InputStreamを初期化してください。");
			}
			this.Encoding = enc;
			this.EncodingConfidence = conf;
			myTextReader = new StreamReader(myStream, this.Encoding);
		}


		// バイナリStreamの先頭最大1024バイトを読み取ってEncodingを判別します。
		public Encoding SniffEncoding(){
			OnMessageRaised(EventLevel.Information, string.Format("文字符号化方式の読み取りを開始します。"));
			int length = myStream.Length > SniffEncodingBufferSize ? SniffEncodingBufferSize : (int)myStream.Length;

			byte[] buffer = new byte[length];
			myStream.Read(buffer, 0, length);
			myStream.Position = 0;

			EncodingSniffer es = new EncodingSniffer(buffer);

			OnMessageRaised(EventLevel.Verbose, string.Format("BOMの読み取りを試みます。"));
			Encoding result = es.SniffEncodingFromBOM();
			if(result != null){
				OnMessageRaised(EventLevel.Information, string.Format("BOMから文字符号化方式を判別しました。: {0} (確定)", result.EncodingName));
				return result;
			}

			OnMessageRaised(EventLevel.Verbose, string.Format("meta要素の読み取りを試みます。"));
			result = es.SniffEncodingFromMeta();
			if(result != null){
				OnMessageRaised(EventLevel.Information, string.Format("meta要素から文字符号化方式を判別しました。: {0} (未確定)", result.EncodingName));
				return result;
			}
			return null;
		}


		// Encodingを後から変更します。
		public void ChangeEncoding(Encoding enc){
			if(this.Encoding == Encoding.Unicode || this.Encoding == Encoding.BigEndianUnicode){
				OnMessageRaised(EventLevel.Information, string.Format("文字符号化方式が指定されましたが、ストリームは既にUTF-16として読み込まれています。指定された文字符号化方式を無視してUTF-16に確定します。: {0}", enc.EncodingName));
				this.EncodingConfidence = EncodingConfidence.Certain;
				return;
			}

			if(enc == Encoding.Unicode || enc == Encoding.BigEndianUnicode){
				OnMessageRaised(EventLevel.Information, string.Format("文字符号化方式UTF-16が指定されましたが、UTF-8が指定されたものとして扱います。: {0}", enc.EncodingName));
				enc = Encoding.UTF8;
			}

			if(enc == this.Encoding){
				OnMessageRaised(EventLevel.Information, string.Format("指定された文字符号化方式は、既知の文字符号化方式と一致しています。文字符号化方式を確定します。: {0}", enc.EncodingName));
				this.EncodingConfidence = EncodingConfidence.Certain;
				return;
			}

			// ToDo: on the fly?
			// ToDO: reload
			OnEncodingChanged(enc);
			return;
		}

		public event EventHandler<ParserEventArgs> EncodingChanged;
		protected void OnEncodingChanged(Encoding enc){
			if(EncodingChanged != null){
				EncodingChanged(this, new ParserEventArgs(EventLevel.Information){Message=string.Format("文字符号化方式を変更する必要があります。: {0}", enc.EncodingName)});
			}
		}


	} //  class InputStream
}



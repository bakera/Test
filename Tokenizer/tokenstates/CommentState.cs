using System;
using System.IO;

namespace Bakera.RedFace{

	public partial class RedFaceParser{

		public class CommentState : TokenizationState{

			public override void Read(Tokenizer t){
				char? c = t.ConsumeChar();
				switch(c){
					case Chars.HYPHEN_MINUS:
						t.ChangeTokenState<CommentEndDashState>();
						return;
					case Chars.NULL:
						t.Parser.OnParseErrorRaised(string.Format("コメント中にNULL文字が含まれています。"));
						t.CurrentCommentToken.Append(Chars.REPLACEMENT_CHARACTER);

						return;
					case null:
						t.Parser.OnParseErrorRaised(string.Format("コメントの解析中に終端に達しました。"));
						t.EmitToken();
						t.UnConsume(1);
						t.ChangeTokenState<DataState>();
						return;
					default:
						t.CurrentCommentToken.Append(c);
						return;
				}
			}
		}
	}
}
using System;
using System.IO;

namespace Bakera.RedFace{

	public partial class RedFaceParser{

		public class CommentEndDashState : TokenizationState{

			public override void Read(Tokenizer t){
				char? c = t.ConsumeChar();
				switch(c){
					case Chars.HYPHEN_MINUS:
						t.ChangeTokenState<CommentEndState>();
						return;
					case Chars.NULL:
						t.Parser.OnParseErrorRaised(string.Format("コメント中にNULL文字が含まれています。"));
						t.CurrentCommentToken.Append(Chars.HYPHEN_MINUS);
						t.CurrentCommentToken.Append(Chars.REPLACEMENT_CHARACTER);

						t.ChangeTokenState<CommentState>();
						return;
					case null:
						t.Parser.OnParseErrorRaised(string.Format("コメントの解析中に終端に達しました。"));
						t.EmitToken();
						t.UnConsume(1);
						t.ChangeTokenState<DataState>();
						return;
					default:
						t.CurrentCommentToken.Append(Chars.HYPHEN_MINUS);
						t.CurrentCommentToken.Append(c);
						t.ChangeTokenState<CommentState>();
						return;
				}
			}
		}
	}
}
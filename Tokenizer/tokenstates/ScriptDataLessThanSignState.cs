using System;
using System.IO;

namespace Bakera.RedFace{

	public partial class RedFaceParser{

		public class ScriptDataLessThanSignState : TokenizationState{

			public override void Read(Tokenizer t){
				char? c = t.ConsumeChar();
				if(c == Chars.SOLIDUS){
					t.TemporaryBuffer = "";
					t.ChangeTokenState<ScriptDataEndTagOpenState>();
					return;
				}
				if(c == Chars.EXCLAMATION_MARK){
					t.EmitToken(new CharacterToken("<!"));
					t.ChangeTokenState<ScriptDataEscapeStartState>();
					return;
				}
				t.EmitToken(new CharacterToken(Chars.LESS_THAN_SIGN));
				t.UnConsume(1);
				t.ChangeTokenState<ScriptDataState>();
				return;
			}
		}
	}
}
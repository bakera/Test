using System;
using System.IO;

namespace Bakera.RedFace{

	public partial class RedFaceParser{

		public class ScriptDataEndTagNameState : TokenizationState{

			public override void Read(Tokenizer t){
				char? c = t.ConsumeChar();

				if(c.IsLatinCapitalLetter()){
					t.CurrentTagToken.Name += c.ToLower();
					t.TemporaryBuffer += c;
					return;
				} else if(c.IsLatinSmallLetter()){
					t.CurrentTagToken.Name += c;
					t.TemporaryBuffer += c;
					return;
 				}

				switch(c){
					case Chars.CHARACTER_TABULATION:
					case Chars.LINE_FEED:
					case Chars.FORM_FEED:
					case Chars.SPACE:
						if(t.IsAppropriateEndTagToken){
							t.ChangeTokenState<BeforeAttributeNameState>();
							return;
						}
						goto default;
					case Chars.SOLIDUS:
						if(t.IsAppropriateEndTagToken){
							t.ChangeTokenState<SelfClosingStartTagState>();
							return;
						}
						goto default;
					case Chars.GREATER_THAN_SIGN:
						if(t.IsAppropriateEndTagToken){
							t.EmitToken();
							t.ChangeTokenState<DataState>();
							return;
						}
						goto default;
					default:{
						t.EmitToken(new CharacterToken("</" + t.TemporaryBuffer));
						t.UnConsume(1);
						t.ChangeTokenState<ScriptDataState>();
						return;
					}
				}
			}
		}
	}
}
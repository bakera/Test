using System;
using System.IO;

namespace Bakera.RedFace{

	public partial class RedFaceParser{

		public class CharacterReferenceInRCDATAState : TokenizationState{
			public override void Read(Tokenizer t){
				t.AdditionalAllowedCharacter = null;
				ReferencedCharacterToken result = ConsumeCharacterReference(t);
				if(result == null){
					t.EmitToken(new CharacterToken(Chars.AMPERSAND));
				} else {
					t.EmitToken(result);
				}
				t.ChangeTokenState<RCDATAState>();
			}
		}
	}

}
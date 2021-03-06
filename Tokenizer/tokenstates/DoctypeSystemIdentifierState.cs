using System;

namespace Bakera.RedFace{
	public class DoctypeSystemIdentifierState<T> : TokenizationState where T : Quoted, new(){

		public override void Read(Tokenizer t){
			char? c = t.ConsumeChar();
			char quote = (new T()).Quote;

			if(c == quote){
				t.ChangeTokenState<AfterDoctypeSystemIdentifierState>();
				return;
			}

			switch(c){
				case Chars.NULL:
					OnMessageRaised(new NullInDoctypeError());
					((DoctypeToken)t.CurrentToken).SystemIdentifier += Chars.REPLACEMENT_CHARACTER;
					return;
				case Chars.GREATER_THAN_SIGN:
					OnMessageRaised(new GreaterThanSignInIdentifierError());
					((DoctypeToken)t.CurrentToken).ForceQuirks = true;
					t.ChangeTokenState<DataState>();
					t.EmitToken();
					return;
				case null:
					OnMessageRaised(new SuddenlyEndAtDoctypeError());
					((DoctypeToken)t.CurrentToken).ForceQuirks = true;
					t.UnConsume(1);
					t.ChangeTokenState<DataState>();
					t.EmitToken();
					return;
				default:
					((DoctypeToken)t.CurrentToken).SystemIdentifier += c;
					return;
			}
		}
	}
}

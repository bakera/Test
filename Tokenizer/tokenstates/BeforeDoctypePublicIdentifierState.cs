using System;

namespace Bakera.RedFace{


	public class BeforeDoctypePublicIdentifierState : TokenizationState{

		public override void Read(Tokenizer t){
			char? c = t.ConsumeChar();

			switch(c){
				case Chars.CHARACTER_TABULATION:
				case Chars.LINE_FEED:
				case Chars.FORM_FEED:
				case Chars.SPACE:
					return;
				case Chars.QUOTATION_MARK:
					t.ChangeTokenState<DoctypePublicIdentifierState<DoubleQuoted>>();
					return;
				case Chars.APOSTROPHE:
					t.ChangeTokenState<DoctypePublicIdentifierState<SingleQuoted>>();
					return;
				case Chars.GREATER_THAN_SIGN:
					OnMessageRaised(new MissingPublicIdentifierError());
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
					OnMessageRaised(new MissingQuoteIdentifierError());
					((DoctypeToken)t.CurrentToken).ForceQuirks = true;
					t.ChangeTokenState<BogusDoctypeState>();
					return;
			}
		}
	}
}

using System;
using System.Xml;

namespace Bakera.RedFace{

	public class AfterAfterBodyInsertionMode : InsertionMode{

		public override void AppendCommentToken(TreeConstruction tree, CommentToken token){
			XmlComment comment = tree.CreateCommentForToken(token);
			tree.Document.AppendChild(comment);
			return;
		}

		public override void AppendDoctypeToken(TreeConstruction tree, DoctypeToken token){
			tree.AppendToken<InBodyInsertionMode>(token);
		}

		public override void AppendCharacterToken(TreeConstruction tree, CharacterToken token){
			if(token.IsSpaceCharacter){
				tree.AppendToken<InBodyInsertionMode>(token);
				return;
			}
			AppendAnythingElse(tree, token);
		}

		public override void AppendStartTagToken(TreeConstruction tree, StartTagToken token){
			switch(token.Name){
			case "html":
				tree.AppendToken<InBodyInsertionMode>(token);
				return;
			}
			AppendAnythingElse(tree, token);
		}

		public override void AppendEndOfFileToken(TreeConstruction tree, EndOfFileToken token){
			tree.Parser.Stop();
			return;
		}

		public override void AppendAnythingElse(TreeConstruction tree, Token token){
			OnMessageRaised(new UnexpectedTokenAfterHtmlError(token.Name));
			tree.ChangeInsertionMode<InBodyInsertionMode>();
			tree.ReprocessFlag = true;
			return;
		}

	}
}

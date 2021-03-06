using System;
using System.Xml;

namespace Bakera.RedFace{

	public class InHeadNoscriptInsertionMode : InsertionMode{

		public override void AppendDoctypeToken(TreeConstruction tree, DoctypeToken token){
			OnMessageRaised(new UnexpectedDoctypeError());
		}

		public override void AppendCharacterToken(TreeConstruction tree, CharacterToken token){
			if(token.IsSpaceCharacter){
				tree.AppendToken<InHeadInsertionMode>(token);
				return;
			}
			AppendAnythingElse(tree, token);
		}

		public override void AppendCommentToken(TreeConstruction tree, CommentToken token){
			tree.AppendToken<InHeadInsertionMode>(token);
		}

		public override void AppendStartTagToken(TreeConstruction tree, StartTagToken token){
			if(token.IsStartTag("html")){
				tree.AppendToken<InBodyInsertionMode>(token);
				return;
			}

			if(token.IsStartTag("basefont", "bgsound", "link", "meta", "noframes", "style")){
				tree.AppendToken<InHeadInsertionMode>(token);
				return;
			}
			if(token.IsStartTag("head", "noscript")){
				OnMessageRaised(new UnexpectedStartTagInHeadNoscriptError(token.Name));
				return;
			}
			AppendAnythingElse(tree, token);
		}

		public override void AppendEndTagToken(TreeConstruction tree, EndTagToken token){
			if(token.IsEndTag("noscript")){
				tree.PopFromStack();
				tree.ChangeInsertionMode<InHeadInsertionMode>();
				return;
			}
			if(token.IsEndTag("br")){
				AppendAnythingElse(tree, token);
				return;
			}
			OnMessageRaised(new UnexpectedEndTagError(token.Name));
			return;
		}

		public override void AppendAnythingElse(TreeConstruction tree, Token token){
			AppendEndTagToken(tree, new FakeEndTagToken(){Name = "noscript"});
			tree.ReprocessFlag = true;
			return;
		}

	}
}

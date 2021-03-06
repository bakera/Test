using System;
using System.Xml;

namespace Bakera.RedFace{

	public class InCaptionInsertionMode : TableRelatedInsertionMode{

		public override void AppendStartTagToken(TreeConstruction tree, StartTagToken token){
			switch(token.Name){
			case "caption":
			case "col":
			case "colgroup":
			case "tbody":
			case "td":
			case "tfoot":
			case "th":
			case "thead":
			case "tr":
				OnMessageRaised(new UnexpectedStartTagInCaptionError(token.Name));
				AppendEndTagToken(tree, new FakeEndTagToken(){Name = "caption"});
				tree.ReprocessFlag = true;
				return;
			}

			AppendAnythingElse(tree, token);
		}


		public override void AppendEndTagToken(TreeConstruction tree, EndTagToken token){
			switch(token.Name){
			case "caption":
				GenerateImpliedEndTags(tree, token);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(token.Name);
				tree.ListOfActiveFormatElements.ClearUpToTheLastMarker();
				tree.ChangeInsertionMode<InTableInsertionMode>();
				return;
			case "table":
				OnMessageRaised(new UnexpectedEndTagError(token.Name));
				AppendEndTagToken(tree, new FakeEndTagToken(){Name = "caption"});
				tree.ReprocessFlag = true;
				return;
			case "body":
			case "col":
			case "colgroup":
			case "html":
			case "tbody":
			case "td":
			case "tfoot":
			case "th":
			case "thead":
			case "tr":
				OnMessageRaised(new UnexpectedEndTagError(token.Name));
				return;
			}
			AppendAnythingElse(tree, token);
		}


		public override void AppendAnythingElse(TreeConstruction tree, Token token){
			tree.AppendToken<InBodyInsertionMode>(token);
		}

	}
}

using System;
using System.Xml;

namespace Bakera.RedFace{


	public class InBodyInsertionMode : InsertionMode{


		private string[] myEndOfFilePermitOpenTags = new string[]{"dd", "dt", "li", "p", "tbody", "td", "tfoot", "th", "thead", "tr", "body", "html"};
		private string[] myBodyEndTagPermitOpenTags = new string[]{"dd", "dt", "li", "optgroup", "option", "p", "rp", "rt", "tbody", "td", "tfoot", "th", "thead", "tr", "body", "html"};
		private string[] myHeadingElements = new string[]{"h1", "h2", "h3", "h4", "h5", "h6"};


		public override void AppendDoctypeToken(TreeConstruction tree, DoctypeToken token){
			OnMessageRaised(new UnexpectedDoctypeError());
			return;
		}

		public override void AppendCommentToken(TreeConstruction tree, CommentToken token){
			tree.AppendCommentForToken(token);
		}

		public override void AppendCharacterToken(TreeConstruction tree, CharacterToken token){
			if(token.IsNULL){
				OnMessageRaised(new NullInDataError());
				return;
			}
			if(token.IsSpaceCharacter){
				Reconstruct(tree, token);
				tree.InsertCharacter(token);
				return;
			}
			Reconstruct(tree, token);
			tree.InsertCharacter(token);
			tree.Parser.FramesetOK = false;
		}

		public override void AppendEndOfFileToken(TreeConstruction tree, EndOfFileToken token){
			string invalidOpenTag = tree.StackOfOpenElements.NotEither(myEndOfFilePermitOpenTags);
			if(invalidOpenTag != null){
				OnMessageRaised(new SuddenlyEndAtElementError(invalidOpenTag));
			}
			tree.Parser.Stop();
			return;
		}


		public override void AppendStartTagToken(TreeConstruction tree, StartTagToken token){
			if(token.IsStartTag("html")){
				OnMessageRaised(new MultipleHtmlElementError());
				XmlElement topElement = tree.StackOfOpenElements[0];
				tree.MergeAttribute(topElement, token);
				return;
			}

			if(token.IsStartTag("base", "basefont", "bgsound", "command", "link", "meta", "noframes", "script", "style", "title")){
				tree.AppendToken<InHeadInsertionMode>(token);
				return;
			}

			if(token.IsStartTag("body")){
				OnMessageRaised(new MultipleBodyElementError());
				XmlElement bodyElement = tree.StackOfOpenElements[1];
				if(bodyElement == null || bodyElement.Name != "body") return;
				tree.Parser.FramesetOK = false;
				tree.MergeAttribute(bodyElement, token);
				return;
			}

			if(token.IsStartTag("frameset")){
				OnMessageRaised(new UnexpectedFramesetElementError());
				XmlElement bodyElement = tree.StackOfOpenElements[1];
				if(bodyElement == null || bodyElement.Name != "body") return;
				if(tree.Parser.FramesetOK == false) return;

				bodyElement.ParentNode.RemoveChild(bodyElement);
				while(tree.StackOfOpenElements.Count > 1) tree.StackOfOpenElements.Pop();
				tree.InsertElementForToken(token);
				tree.ChangeInsertionMode<InFramesetInsertionMode>();
				return;
			}

			if(token.IsStartTag("address", "article", "aside", "blockquote", "center", "details", "dir", "div", "dl", "fieldset", "figcaption", "figure", "footer", "header", "hgroup", "menu", "nav", "ol", "p", "section", "summary", "ul")){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag(myHeadingElements)){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				if(tree.StackOfOpenElements.IsCurrentNameMatch(myHeadingElements)){
					OnMessageRaised(new NestedHeadingElementError(tree.CurrentNode.Name, token.Name));
					tree.StackOfOpenElements.Pop();
				}
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag("pre", "listing")){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				tree.Parser.FramesetOK = false;
				tree.IgnoreNextLineFeed = true;
				return;
			}

			if(token.IsStartTag("form")){
				if(tree.FormElementPointer != null){
					OnMessageRaised(new NestedFormElementError());
					return;
				}
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				XmlElement form = tree.InsertElementForToken(token);
				tree.FormElementPointer = form;
				return;
			}

			if(token.IsStartTag("li")){
				tree.Parser.FramesetOK = false;
				foreach(XmlElement e in tree.StackOfOpenElements){
					if(StackOfElements.IsNameMatch(e, "li")){
						EndTagHadBeSeen(tree, "li");
						break;
					}
					if(StackOfElements.IsSpecialElement(e)){
						if(!StackOfElements.IsNameMatch(e, "address", "div", "p")){
							break;
						}
					}
				}
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag("dd", "dt")){
				tree.Parser.FramesetOK = false;
				foreach(XmlElement e in tree.StackOfOpenElements){
					if(StackOfElements.IsNameMatch(e, "dd", "dt")){
						EndTagHadBeSeen(tree, token.Name);
						break;
					}
					if(StackOfElements.IsSpecialElement(e)){
						if(!StackOfElements.IsNameMatch(e, "address", "div", "p")){
							break;
						}
					}
				}
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag("plaintext")){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				tree.Parser.ChangeTokenState<PLAINTEXTState>();
				return;
			}

			if(token.IsStartTag("button")){
				if(tree.StackOfOpenElements.HaveElementInScope("button")){
					OnMessageRaised(new NestedButtonElementError());
					EndTagHadBeSeen(tree, "button");
					tree.ReprocessFlag = true;
					return;
				}
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				tree.Parser.FramesetOK = false;
				return;
			}

			if(token.IsStartTag("a")){
				var list = tree.ListOfActiveFormatElements;
				var stack = tree.StackOfOpenElements;
				int index = list.GetAfterMarkerIndexByName("a");
				if(index >=0){
					XmlElement aElement = list.GetAfterMarkerByAfterIndex(index);
					OnMessageRaised(new NestedAnchorElementError());
					EndTagHadBeSeen(tree, "a");
					stack.Remove(aElement);
					list.Remove(aElement);
				}
				Reconstruct(tree, token);
				XmlElement e = tree.InsertElementForToken(token);
				tree.ListOfActiveFormatElements.Push(e, token);
				return;
			}

			if(token.IsStartTag("b", "big", "code", "em", "font", "i", "s", "small", "strike", "strong", "tt", "u")){
				Reconstruct(tree, token);
				XmlElement e = tree.InsertElementForToken(token);
				tree.ListOfActiveFormatElements.Push(e, token);
				return;
			}

			if(token.IsStartTag("nobr")){
				Reconstruct(tree, token);
				if(!tree.StackOfOpenElements.HaveElementInScope(token.Name)){
					OnMessageRaised(new NestedNobrElementError());
					FormatEndTagHadBeSeen(tree, token, "nobr");
					Reconstruct(tree, token);
					return;
				}
				XmlElement e = tree.InsertElementForToken(token);
				tree.ListOfActiveFormatElements.Push(e, token);
				return;
			}

			if(token.IsStartTag("applet", "marquee", "object")){
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				tree.Parser.FramesetOK = false;
				tree.ListOfActiveFormatElements.InsertMarker();
				return;
			}

			if(token.IsStartTag("table")){
				if(tree.Document.DocumentMode == DocumentMode.Quirks && tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				tree.Parser.FramesetOK = false;
				tree.ChangeInsertionMode<InTableInsertionMode>();
				return;
			}

			if(token.IsStartTag("area", "br", "embed", "img", "keygen", "wbr")){
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				tree.PopFromStack();
				tree.AcknowledgeSelfClosingFlag(token);
				tree.Parser.FramesetOK = false;
				return;
			}

			if(token.IsStartTag("input")){
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				tree.PopFromStack();
				tree.AcknowledgeSelfClosingFlag(token);
				if(!token.IsHiddenType()){
					tree.Parser.FramesetOK = false;
				}
				return;
			}

			if(token.IsStartTag("param", "source", "track")){
				tree.InsertElementForToken(token);
				tree.AcknowledgeSelfClosingFlag(token);
				tree.PopFromStack();
				return;
			}

			if(token.IsStartTag("hr")){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				tree.InsertElementForToken(token);
				tree.AcknowledgeSelfClosingFlag(token);
				tree.PopFromStack();
				return;
			}

			if(token.IsStartTag("image")){
				OnMessageRaised(new ImageElementError());
				token.Name = "img";
				tree.ReprocessFlag = true;
				return;
			}

			if(token.IsStartTag("isindex")){
				OnMessageRaised(new IsindexElementError());
				XmlElement node = tree.FormElementPointer;
				if(node != null) return;
				tree.AcknowledgeSelfClosingFlag(token);

				StartTagHadBeSeen(tree, "form");
				XmlElement form = (XmlElement)tree.CurrentNode;
				string actionAttrValue = token.GetAttributeValue("action");
				if(actionAttrValue != null) form.SetAttribute("action", actionAttrValue);
				StartTagHadBeSeen(tree, "hr");
				StartTagHadBeSeen(tree, "label");
				string promptAttrValue = token.GetAttributeValue("prompt");
				if(promptAttrValue == null){
					tree.InsertText("This is a searchable index. Enter search keywords:");
				} else {
					tree.InsertText(promptAttrValue);

				}
				StartTagHadBeSeen(tree, "input");
				XmlElement input = (XmlElement)tree.CurrentNode["input"];
				input.SetAttribute("name", "isindex");
				foreach(AttributeToken at in token.Attributes){
					if(at.Name == "name" || at.Name == "action" || at.Name == "prompt") continue;
					input.SetAttribute(at.Name, at.Value);
				}
				EndTagHadBeSeen(tree, "label");
				StartTagHadBeSeen(tree, "hr");
				EndTagHadBeSeen(tree, "form");
				return;
			}

			if(token.IsStartTag("textarea")){
				tree.InsertElementForToken(token);
				tree.IgnoreNextLineFeed = true;
				tree.Parser.ChangeTokenState<RCDATAState>();
				tree.OriginalInsertionMode = tree.CurrentInsertionMode;
				tree.Parser.FramesetOK = false;
				tree.ChangeInsertionMode<TextInsertionMode>();
				return;
			}

			if(token.IsStartTag("xmp")){
				if(tree.StackOfOpenElements.HaveElementInButtonScope("p")){
					EndTagPHadBeSeen(tree, token);
				}
				Reconstruct(tree, token);
				tree.Parser.FramesetOK = false;
				GenericRawtextElementParsingAlgorithm(tree, token);
				return;
			}

			if(token.IsStartTag("iframe")){
				tree.Parser.FramesetOK = false;
				GenericRawtextElementParsingAlgorithm(tree, token);
				return;
			}

			// start tag whose tag name is "noscript", if the scripting flag is enabled
			if(token.IsStartTag("noembed")){
				GenericRawtextElementParsingAlgorithm(tree, token);
				return;
			}

			if(token.IsStartTag("select")){
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				tree.Parser.FramesetOK = false;
				if(tree.CurrentInsertionMode is TableRelatedInsertionMode){
					tree.ChangeInsertionMode<InSelectInTableInsertionMode>();
				} else {
					tree.ChangeInsertionMode<InSelectInsertionMode>();
				}
				return;
			}

			if(token.IsStartTag("optgroup", "option")){
				if(tree.CurrentNode.Name == "option") EndTagHadBeSeen(tree, "option");
				Reconstruct(tree, token);
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag("rp", "rt")){
				if(tree.StackOfOpenElements.HaveElementInScope("ruby")){
					GenerateImpliedEndTags(tree, token);
					if(!tree.StackOfOpenElements.IsCurrentNameMatch("ruby")){
						OnMessageRaised(new DirectParentRequiredError(token.Name, "ruby", tree.CurrentNode.Name));
						tree.StackOfOpenElements.PopUntilSameTagName("ruby");
					}
				}
				tree.InsertElementForToken(token);
				return;
			}

			if(token.IsStartTag("math")){
				Reconstruct(tree, token);
				token.AdjustMathMLAttributes();
				token.AdjustForeignAttributes();
				XmlElement result = tree.CreateElementForToken(token, Document.MathMLNamespace);
				tree.InsertElement(result);
				if(token.SelfClosing){
					tree.AcknowledgeSelfClosingFlag(token);
					tree.PopFromStack();
				}
				return;
			}

			if(token.IsStartTag("svg")){
				Reconstruct(tree, token);
				token.AdjustSVGAttributes();
				token.AdjustForeignAttributes();
				XmlElement result = tree.CreateElementForToken(token, Document.SVGNamespace);
				tree.InsertElement(result);
				if(token.SelfClosing){
					tree.AcknowledgeSelfClosingFlag(token);
					tree.PopFromStack();
				}
				return;
			}

			if(token.IsStartTag("caption", "col", "colgroup", "frame", "head", "tbody", "td", "tfoot", "th", "thead", "tr")){
				OnMessageRaised(new ElementContextError(token.Name));
				return;
			}

			// Any Other Start Tags.
			Reconstruct(tree, token);
			tree.InsertElementForToken(token);
			return;

		}


		public override void AppendEndTagToken(TreeConstruction tree, EndTagToken token){

			if(token.IsEndTag("body")){
				if(!tree.StackOfOpenElements.HaveElementInScope("body")){
					OnMessageRaised(new UnexpectedEndTagError(token.Name));
					return;
				}
				string invalidOpenTag = tree.StackOfOpenElements.NotEither(myBodyEndTagPermitOpenTags);
				if(invalidOpenTag != null){
					OnMessageRaised(new MissingEndTagError(invalidOpenTag));
				}
				tree.ChangeInsertionMode<AfterBodyInsertionMode>();
				return;
			}

			if(token.IsEndTag("html")){
				EndTagHadBeSeen(tree, "body");
				tree.ReprocessFlag = true;
				return;
			}

			if(token.IsEndTag("address", "article", "aside", "blockquote", "button", "center", "details", "dir", "div", "dl", "fieldset", "figcaption", "figure", "footer", "header", "hgroup", "listing", "menu", "nav", "ol", "pre", "section", "summary", "ul")){
				if(!tree.StackOfOpenElements.HaveElementInScope(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(token.Name);
				return;
			}

			if(token.IsEndTag("form")){
				XmlElement node = tree.FormElementPointer;
				tree.FormElementPointer = null;
				if(node == null || !tree.StackOfOpenElements.Contains(node)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token);
				if(tree.CurrentNode != node){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				} else {
					tree.StackOfOpenElements.Pop();
				}
				return;
			}

			if(token.IsEndTag("p")){
				EndTagPHadBeSeen(tree, token);
				return;
			}

			if(token.IsEndTag("li")){
				if(!tree.StackOfOpenElements.HaveElementInListItemScope(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token, token.Name);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(token.Name);
				return;
			}

			if(token.IsEndTag("dd", "dt")){
				if(!tree.StackOfOpenElements.HaveElementInScope(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token, token.Name);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(token.Name);
				return;
			}

			if(token.IsEndTag(myHeadingElements)){
				if(!tree.StackOfOpenElements.HaveElementInScope(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(myHeadingElements);
				return;
			}

			if(token.IsEndTag("sarcasm")){
				OnMessageRaised(new SarcasmEndTagInformation());
				AnyOtherEndTag(tree, token);
				return;
			}

			if(token.IsEndTag("a", "b", "big", "code", "em", "font", "i", "nobr", "s", "small", "strike", "strong", "tt", "u")){
				FormatEndTagHadBeSeen(tree, token, token.Name);
				return;
			}

			if(token.IsEndTag("applet", "marquee", "object")){
				if(!tree.StackOfOpenElements.HaveElementInScope(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
					return;
				}
				GenerateImpliedEndTags(tree, token);
				if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
					OnMessageRaised(new LonlyEndTagError(token.Name));
				}
				tree.StackOfOpenElements.PopUntilSameTagName(token.Name);
				tree.ListOfActiveFormatElements.ClearUpToTheLastMarker();
				return;
			}

			if(token.IsEndTag("br")){
				OnMessageRaised(new BrEndTagError());
				StartTagHadBeSeen(tree, "br");
				return;
			}

			AnyOtherEndTag(tree, token);
		}


// private

		// 補える終了タグを全部補う処理
		// 補えないタイプの終了タグが出現したときにのみ呼ばれる
		private void GenerateImpliedEndTags(TreeConstruction tree, Token token){
			GenerateImpliedEndTags(tree, token, new string[0]);
			return;
		}


// tags had be seen

		private void StartTagHadBeSeen(TreeConstruction tree, string name){
			AppendStartTagToken(tree, new FakeStartTagToken(){Name = name});
		}

		private void EndTagHadBeSeen(TreeConstruction tree, string name){
			AppendEndTagToken(tree, new FakeEndTagToken(){Name = name});
		}

		private void EndTagPHadBeSeen(TreeConstruction tree, Token token){
			string tagName = "p";
			if(!tree.StackOfOpenElements.HaveElementInButtonScope(tagName)){
				OnMessageRaised(new LonlyEndTagError(token.Name));
				StartTagHadBeSeen(tree, "p");
				tree.ReprocessFlag = true;
				return;
			}
			GenerateImpliedEndTags(tree, token, tagName);
			if(!tree.StackOfOpenElements.IsCurrentNameMatch(tagName)){
				OnMessageRaised(new LonlyEndTagError(tagName));
			}
			tree.StackOfOpenElements.PopUntilSameTagName(tagName);
			return;
		}

		private void AnyOtherEndTag(TreeConstruction tree, Token token){
			foreach(XmlElement node in tree.StackOfOpenElements){
				if(StackOfElements.IsNameMatch(node, token.Name)){
					GenerateImpliedEndTags(tree, token, token.Name);
					if(!tree.StackOfOpenElements.IsCurrentNameMatch(token.Name)){
						OnMessageRaised(new LonlyEndTagError(token.Name));
					}
					tree.StackOfOpenElements.PopUntilSameElement(node);
					break;
				} else {
					if(StackOfElements.IsSpecialElement(node)){
						OnMessageRaised(new LonlyEndTagError(token.Name));
						return;
					}
				}
			}
			return;
		}

		private void FormatEndTagHadBeSeen(TreeConstruction tree, Token token, string tagName){
			ListOfElements list = tree.ListOfActiveFormatElements;
			StackOfElements stack = tree.StackOfOpenElements;
			int outerLoopCounter = 0;

			while(outerLoopCounter < 8){
				outerLoopCounter++;
				int formattingElementItemIndex = list.GetAfterMarkerIndexByName(token.Name);
				if(formattingElementItemIndex < 0){
					AnyOtherEndTag(tree, token);
					return;
				}

				XmlElement formattingElement = list.GetAfterMarkerByAfterIndex(formattingElementItemIndex);

				if(!stack.IsInclude(formattingElement)){
					OnMessageRaised(new DisproportionalFormatEndTagError(token.Name));
					list.RemoveAfterMarkerByAfterIndex(formattingElementItemIndex);
					return;
				}

				if(!stack.HaveElementInScope(formattingElement.Name)){
					OnMessageRaised(new DisproportionalFormatEndTagError(token.Name));
					return;
				}

				if(tree.CurrentNode != formattingElement){
					OnMessageRaised(new DisproportionalFormatEndTagError(token.Name));
					// エラーだが処理は続行
				}

				XmlElement furthestBlock = stack.GetFurthestBlock(formattingElement);
				if(furthestBlock == null){
					stack.PopUntilSameElement(formattingElement);
					list.RemoveAfterMarkerByAfterIndex(formattingElementItemIndex);
					return;
				}

				XmlElement commonAncestor = stack.GetAncestor(formattingElement);

				// bookmark
				int bookmarkPosition = list.GetIndexByElement(formattingElement);

				XmlElement node = furthestBlock;
				XmlElement lastNode = furthestBlock;

				int innerLoopCounter = 0;
				while(innerLoopCounter < 3){
					innerLoopCounter++;
					node = stack.GetAncestor(node);
					int idx = list.GetIndexByElement(node);
					if(idx < 0){
						node = stack.Remove(node);
						continue;
					}
					if(stack.IsFormattingElement(node)){
						break;
					}
					TagToken creator = tree.GetToken(node);
					XmlElement newNode = tree.CreateElementForToken(creator);
					list[idx] = newNode;
					stack.Replace(node, newNode);
					node = newNode;
					if(lastNode == furthestBlock){
						bookmarkPosition = idx+1;
					}
					node.AppendChild(lastNode);
					lastNode = node;
				}
				if(stack.IsTableRealtedElement(commonAncestor)){
					stack.FosterParent(lastNode);
				} else {
					commonAncestor.AppendChild(lastNode);
				}

				TagToken formatElementCreator = tree.GetToken(node);
				XmlElement newFormattingElement = tree.CreateElementForToken(formatElementCreator);
				foreach(XmlNode x in furthestBlock.ChildNodes){
					newFormattingElement.AppendChild(x);
				}
				furthestBlock.AppendChild(newFormattingElement);

				list.Remove(formattingElement);
				list.Insert(bookmarkPosition, newFormattingElement);

				stack.Remove(formattingElement);
				stack.InsertBelow(furthestBlock, newFormattingElement);

			}
			return;
		}




// reconstruct 

		public void Reconstruct(TreeConstruction tree, Token token){
			ListOfElements list = tree.ListOfActiveFormatElements;
			StackOfElements stack = tree.StackOfOpenElements;
			if(list.Length == 0) return;

			XmlElement lastEntry = list[list.Length - 1];
			if(lastEntry == null) return;
			if(stack.IsInclude(lastEntry)) return;

			// step4～7は要するに「stackに含まれる要素やmarkerを除いた、最も先祖にあるentryを取得」という処理をする。
			// index = 0 なら最上位なので、そのentryを使用する。
			// 一つ上を見て、そのentryがstackもしくはmarkerなら、そのentryを使用する。
			// step7が別の目的にも使いまわされているのが読みにくい原因。
			int index = list.Length-1;
			while(index > 0){
				XmlElement parentEntry = list[index-1];
				if(parentEntry == null || stack.IsInclude(parentEntry)){
					break;
				}
				index--;
			}

			XmlElement entry = null;
			while(index < list.Length){
				entry = list[index];
				TagToken creator = tree.GetToken(entry);
				XmlElement e = tree.CreateElementForToken(creator);
				tree.InsertElement(e);
				list[index] = e;
				index++;
			}
			return;
		}

	}
}

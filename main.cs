using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Bakera.RedFace{

	public class App{

		private EventLevel myEventLevel = EventLevel.Information;
		private EventLevel EventLevel{
			get{return myEventLevel;}
			set{myEventLevel = value;}
		}
		private NameValueCollection myArgs = new NameValueCollection();
		private string myTargetPath = null;


		public static int Main(string[] args){
			try{
				App app = new App();
				app.ParseArgs(args);
				return app.Execute(args);
			} catch(Exception e){
				Console.WriteLine(e);
				return 1;
			}
		}


		private int ParseFromUri(string uri){

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
			request.AllowAutoRedirect = false;
			request.UserAgent = "RedFace/0.1";

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();

			RedFaceParser p = new RedFaceParser();
			p.ParserEventRaised += WriteEvent;

			string charsetName = EncodingSniffer.ExtractEncodingNameFromMetaElement(response.ContentType);
			if(!string.IsNullOrEmpty(charsetName)){
				Console.WriteLine("HTTP応答ヘッダで文字符号化方式が指定されています。: {0}", charsetName);
				p.SetForceEncoding(charsetName);
			}
			using(Stream data = response.GetResponseStream()){
				p.Parse(data);
			}
			PrintResult(p);
			return 0;
		}

		private int ParseFromFile(string path){
			FileInfo file = new FileInfo(path);
			if(file.Exists){
				return ParseFromFile(file);
			}
			DirectoryInfo dir = new DirectoryInfo(path);
			if(dir.Exists){
				return ParseFromDirectory(dir);
			}
			Console.WriteLine("指定されたファイルがみつかりませんでした: {0}", file.FullName);
			return 1;
		}

		private int ParseFromFile(FileInfo file){
			Console.WriteLine("ファイル: <{0}> をパースします。", file.FullName);
			RedFaceParser p = new RedFaceParser();
			p.ParserEventRaised += WriteEvent;

			using(FileStream fs = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)){
				p.Parse(fs);
			}
			PrintResult(p);
			return 0;
		}

		private int ParseFromDirectory(DirectoryInfo dir){
			foreach(DirectoryInfo subdir in dir.GetDirectories()){
				ParseFromDirectory(subdir);
			}
			foreach(FileInfo file in dir.GetFiles()){
				ParseFromFile(file);
			}
			return 0;
		}


		private int Execute(string[] args){
			if(myTargetPath == null){
				Console.WriteLine("対象のファイル名もしくはURLを指定してください。");
				return 1;
			}

			if(myTargetPath.StartsWith("http://") || myTargetPath.StartsWith("https://")){
				return ParseFromUri(myTargetPath);
			} else {
				return ParseFromFile(myTargetPath);
			}
		}

		private void PrintResult(RedFaceParser p){
			var logs = p.GetLogs();
			foreach(ParserLog log in logs){
				Console.WriteLine("{0}行{1}文字: {2}", log.Line.Number, log.ColumnNumber, log.Message);
				Console.WriteLine(" {0}", log.Line.Data);
			}
			Console.WriteLine("パース開始: {0}", p.StartTime);
			Console.WriteLine("パース終了: {0}", p.EndTime);
			Console.WriteLine("パース時間: {0}", p.EndTime - p.StartTime);
			Console.WriteLine();
			Console.WriteLine("========");
//			Console.WriteLine(p.Document.OuterXml);
		}


		public void WriteEvent(Object sender, ParserEventArgs e){
			if(e.Level >= myEventLevel){
				Console.Write("[{0}] ", e.Level);
				Console.Write("({0}) ", e.Message.GetType().Name);
				if(!string.IsNullOrEmpty(e.Message.Message)) Console.WriteLine(e.Message.Message);
				if(sender is RedFaceParser && e.Level > EventLevel.Information){
					if(e.OriginalSender != null){
						Console.WriteLine(" {0}:", e.OriginalSender.GetType().Name);
					}
					RedFaceParser parser = (RedFaceParser)sender;
					Console.Write(" {0}", parser.InputStream.GetRecentString(40));
					string allstr = parser.InputStream.GetAllString();
					string[] lines = allstr.Split('\n');
					Console.Write(" ({0}行目", lines.Length);
					Console.Write(" {0}文字目)", lines[lines.Length-1].Length);

					Console.WriteLine();
				}
			}
		}


		// コマンドライン引数を解析してNameValueCollectionに格納します。
		private void ParseArgs(string[] args){
			for(int i=0; i < args.Length; i++){
				string argName = args[i];
				if(argName.StartsWith("-")){
					if(argName == "-v"){
						this.EventLevel = EventLevel.Verbose;
					}
				} else {
					myTargetPath = argName;
				}
			}

		}

	}

}




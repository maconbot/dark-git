using System;
namespace Dark1Git
{
	public class HistoryItem
	{
		public string Hash;
		public string Author;
		public string Date;
		public string Message;
		
		public HistoryItem (string hash, string author, string date, string message)
		{
			Hash = hash;
			Author = author;
			Date = date;
			Message = message;
		}
		public override string ToString ()
		{
			return string.Format ("[HistoryItem]["+Author+"]: "+Message);
		}
	}
}


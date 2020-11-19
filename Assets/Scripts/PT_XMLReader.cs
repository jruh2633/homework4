using UnityEngine;
using System.Collections;
using System.Collections.Generic;



		


[System.Serializable]
public class PT_XMLReader {
	static public bool		SHOW_COMMENTS = false;

	
	public string xmlText;
	public PT_XMLHashtable xml;
	
	
	
	
	public void Parse(string eS) {
		xmlText = eS;
		xml = new PT_XMLHashtable();
		Parse(eS, xml);
	}
	
	
	void Parse(string eS, PT_XMLHashtable eH) {
		eS = eS.Trim();
		while(eS.Length > 0) {
			eS = ParseTag(eS, eH);
			eS = eS.Trim();
		}
	}
	
	
	string ParseTag(string eS, PT_XMLHashtable eH) {
		
		int ndx = eS.IndexOf("<");
		int end, end1, end2, end3;
		if (ndx == -1) {
			
			end3 = eS.IndexOf(">"); 
			if (end3 == -1) {
				
				eS = eS.Trim(); 
				
				eH.text = eS;
			}
			return(""); 
		}
		
		if (eS[ndx+1] == '?') {
			
			int ndx2 = eS.IndexOf("?>");
			string header = eS.Substring(ndx, ndx2-ndx+2);
			
			eH.header = header;
			return(eS.Substring(ndx2+2));
		}
		
		if (eS[ndx+1] == '!') {
			
			int ndx2 = eS.IndexOf("-->");
			string comment = eS.Substring(ndx, ndx2-ndx+3);
			if (SHOW_COMMENTS) Debug.Log("XMl Comment: "+comment);
			
			return(eS.Substring(ndx2+3));
		}
		
		
										
		end1 = eS.IndexOf(" ", ndx);	
		end2 = eS.IndexOf("/", ndx);	
		end3 = eS.IndexOf(">", ndx);	
		if (end1 == -1) end1 = int.MaxValue;
		if (end2 == -1) end2 = int.MaxValue;
		if (end3 == -1) end3 = int.MaxValue;
		
		
		end = Mathf.Min(end1, end2, end3);
		string tag = eS.Substring(ndx+1, end-ndx-1);
		
		
		if (!eH.ContainsKey(tag)) {
			eH[tag] = new PT_XMLHashList();
		}
		
		PT_XMLHashList arrL = eH[tag] as PT_XMLHashList;
		
		PT_XMLHashtable thisHash = new PT_XMLHashtable();
		arrL.Add(thisHash);
		
		
		string atts = "";
		if (end1 < end3) {
			try {
				atts = eS.Substring(end1, end3-end1);
			}
			catch(System.Exception ex) {
				Debug.LogException(ex);
				Debug.Log("break");
			}
		}
		
		string att, val;
		int eqNdx, spNdx;
		while (atts.Length > 0) {
			atts = atts.Trim();
			eqNdx = atts.IndexOf("=");
			if (eqNdx == -1) break;
			
			att = atts.Substring(0,eqNdx);
			spNdx = atts.IndexOf(" ",eqNdx);
			if (spNdx == -1) { 
				val = atts.Substring(eqNdx+1);
				if (val[val.Length-1] == '/') { 
					val = val.Substring(0,val.Length-1);
				}
				atts = "";
			} else { 
				val = atts.Substring(eqNdx+1, spNdx - eqNdx - 2);
				atts = atts.Substring(spNdx);
			}
			val = val.Trim('\"');
			
			thisHash.attSet(att, val);
		}
		
		
		
		string subs = "";
		string leftoverString = "";
		
		bool singleLine = (end2 == end3-1);
		if (!singleLine) { 
			
			int close = eS.IndexOf("</"+tag+">");

			if (close == -1) {
				Debug.Log("XMLReader ERROR: XML not well formed. Closing tag </"+tag+"> missing.");
				return("");
			}
			subs = eS.Substring(end3+1, close-end3-1);
			leftoverString = eS.Substring( eS.IndexOf(">",close)+1 );
		} else {
			leftoverString = eS.Substring(end3+1);
		}
		
		subs = subs.Trim();
		
		if (subs.Length > 0) {
			Parse(subs, thisHash);
		}
		
		
		leftoverString = leftoverString.Trim();
		return(leftoverString);
	
	}
	
}



public class PT_XMLHashList {
	public ArrayList list = new ArrayList();
	
	public PT_XMLHashtable this[int s] {
		get {
			return(list[s] as PT_XMLHashtable);
		}
		set {
			list[s] = value;
		}
	}
	
	public void Add(PT_XMLHashtable eH) {
		list.Add(eH);
	}
	
	public int Count {
		get {
			return(list.Count);
		}
	}
	
	public int length {
		get {
			return(list.Count);
		}
	}
}


public class PT_XMLHashtable {
	
	public List<string>				keys = new List<string>();
	public List<PT_XMLHashList>		nodesList = new List<PT_XMLHashList>();
	public List<string>				attKeys = new List<string>();
	public List<string>				attributesList = new List<string>();
	
	public PT_XMLHashList Get(string key) {
		int ndx = Index(key);
		if (ndx == -1) return(null);
		return( nodesList[ndx] );
	}
	
	public void Set(string key, PT_XMLHashList val) {
		int ndx = Index(key);
		if (ndx != -1) {
			nodesList[ndx] = val;
		} else {
			keys.Add(key);
			nodesList.Add(val);
		}
	}
	
	public int Index(string key) {
		return(keys.IndexOf(key));
	}
	
	public int AttIndex(string attKey) {
		return(attKeys.IndexOf(attKey));
	}
	
	
	public PT_XMLHashList this[string s] {
		get {
			return( Get(s) );
		}
		set {
			Set( s, value );
		}
	}
	
	public string att(string attKey) {
		int ndx = AttIndex(attKey);
		if (ndx == -1) return("");
		return( attributesList[ndx] );
	}
	
	public void attSet(string attKey, string val) {
		int ndx = AttIndex(attKey);
		if (ndx == -1) {
			attKeys.Add(attKey);
			attributesList.Add(val);
		} else {
			attributesList[ndx] = val;
		}
	}
	
	public string text {
		get {
			int ndx = AttIndex("@");
			if (ndx == -1) return( "" );
			return( attributesList[ndx] );
		}
		set {
			int ndx = AttIndex("@");
			if (ndx == -1) {
				attKeys.Add("@");
				attributesList.Add(value);
			} else {
				attributesList[ndx] = value;
			}
		}
	}
	
	
	public string header {
		get {
			int ndx = AttIndex("@XML_Header");
			if (ndx == -1) return( "" );
			return( attributesList[ndx] );
		}
		set {
			int ndx = AttIndex("@XML_Header");
			if (ndx == -1) {
				attKeys.Add("@XML_Header");
				attributesList.Add(value);
			} else {
				attributesList[ndx] = value;
			}
		}
	}
	
	
	public string nodes {
		get {
			string s = "";
			foreach (string key in keys) {
				s += key+"   ";
			}
			return(s);
		}
	}
	
	public string attributes {
		get {
			string s = "";
			foreach (string attKey in attKeys) {
				s += attKey+"   ";
			}
			return(s);
		}
	}
	
	public bool ContainsKey(string key) {
		return( Index(key) != -1 );
	}
	
	public bool ContainsAtt(string attKey) {
		return( AttIndex(attKey) != -1 );
	}
	
	public bool HasKey(string key) {
		return( Index(key) != -1 );
	}
	
	public bool HasAtt(string attKey) {
		return( AttIndex(attKey) != -1 );
	}
	
}





						
						
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// What chat did, for the screen (chat can't be answered): "Bob summoned skeleton".
// The same thing again in a row joins the line: "Bob, Ann, Cid +2 summoned skeleton". UI.chatText
public static class ChatFeed
{
    const float stay = 6; // seconds a line stays
    const int lines = 4;
    class Line{public string what; public List<string> who = new(); public float at;}
    static readonly List<Line> feed = new();

    public static void Add(string who, string what){
        var last = feed.Count>0 ? feed[feed.Count-1] : null;
        if(last!=null && last.what==what && Time.time<last.at+stay){if(!last.who.Contains(who))last.who.Add(who); last.at = Time.time; return;}
        feed.Add(new(){what = what, who = {who}, at = Time.time});
        if(feed.Count>lines)feed.RemoveAt(0);
    }
    public static string Text(){
        feed.RemoveAll(l=>Time.time>l.at+stay);
        return string.Join("\n", feed.ConvertAll(l=>Who(l.who)+" "+l.what));
    }
    static string Who(List<string> w)=>w.Count<=3 ? string.Join(", ", w) : string.Join(", ", w.GetRange(0,3))+" +"+(w.Count-3);
}

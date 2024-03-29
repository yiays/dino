﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class GameData : MonoBehaviour
{
    public List<Highscore> Highscores = new List<Highscore>();
    private static readonly string urltemplate = "https://newsimland.com/~Asriel-Repp/{0}.php?get&{1}";
    public User user;
    public Leaderboard leaderboard;
    public List<Message> messages = new List<Message>();
    public Chat chat;
    
    void Start(){
        var chatobject = GameObject.Find("GUI/IngameCanvas/Chat");
        chatobject.SetActive(false);
        chat = chatobject.GetComponent<Chat>();
    }
    
    public void Login(string username, string password)
    {
        var @params = String.Format("login&username={0}&password={1}",HttpUtility.UrlEncode(username),HttpUtility.UrlEncode(password));
        StartCoroutine(Get(
            String.Format(urltemplate,"account",@params),
            (UnityWebRequest webRequest) => {
                TextMeshProUGUI txtLoginDesc = GameObject.Find("GUI/LoginCanvas/Login/Form/Desc").GetComponent<TextMeshProUGUI>();
                if(webRequest.isNetworkError){
                    txtLoginDesc.text = "<color=red>There was a network error! Please try again.</color>";
                    return false;
                }
                try{
                    Account result = JsonConvert.DeserializeObject<Account>(webRequest.downloadHandler.text);
                    if(result.success){
                        user = result.user;
                        user.loggedin = true;
                        GameObject.Find("GUI/LoginCanvas").SetActive(false);
                        GameObject.Find("GUI/IngameCanvas/HighScore").GetComponent<TextMeshProUGUI>().text = user.score.ToString();
                        GameObject.Find("GameManager").GetComponent<GameManager>().dinofocus = true;
                        GetMessages();
                        StartCoroutine(GetMessagesLoop());
                    }else{
                        txtLoginDesc.text = "<color=red>"+result.msg+"</color>";
                    }
                    return true;
                }
                catch{
                    txtLoginDesc.text = "<color=red>"+webRequest.downloadHandler.text+"</color>";
                    return false;
                }
            }
        ));
    }
    public void Register(string username, string password){
        var @params = String.Format("register&username={0}&password={1}",HttpUtility.UrlEncode(username),HttpUtility.UrlEncode(password));
        StartCoroutine(Get(
            String.Format(urltemplate,"account",@params),
            (UnityWebRequest webRequest) => {
                TextMeshProUGUI txtLoginDesc = GameObject.Find("GUI/LoginCanvas/Login/Form/Desc").GetComponent<TextMeshProUGUI>();
                if(webRequest.isNetworkError){
                    txtLoginDesc.text = "<color=red>There was a network error! Please try again.</color>";
                    return false;
                }
                try{
                    Account result = JsonConvert.DeserializeObject<Account>(webRequest.downloadHandler.text);
                    if(result.success){
                        user = result.user;
                        user.loggedin = true;
                        GameObject.Find("GUI/LoginCanvas").SetActive(false);
                    }else{
                        txtLoginDesc.text = "<color=red>"+result.msg+"</color>";
                    }
                    return true;
                }
                catch{
                    txtLoginDesc.text = "<color=red>"+webRequest.downloadHandler.text+"</color>";
                    return false;
                }
            }
        ));
    }
    public void GetLeaderboard(){
        GameObject.Find("GUI/GameOverCanvas/Leaderboard/Viewport/Content/txtLeaderboard").GetComponent<TextMeshProUGUI>().text = "Loading... Please wait.";
        StartCoroutine(Get(
            String.Format(urltemplate,"leaderboard",""),
            (UnityWebRequest webRequest) => {
                TextMeshProUGUI txtLeaderboard = GameObject.Find("GUI/GameOverCanvas/Leaderboard/Viewport/Content/txtLeaderboard").GetComponent<TextMeshProUGUI>();
                txtLeaderboard.text = "";
                if(webRequest.isNetworkError){
                    txtLeaderboard.text = "<color=red>There was a network error! Please try again.</color>";
                    return false;
                }
                try{
                    leaderboard = JsonConvert.DeserializeObject<Leaderboard>(webRequest.downloadHandler.text);
                    for(var i=0;i<leaderboard.users.Length;i++){
                        txtLeaderboard.text += "<line-height=0.001em><align=left>"+(i+1).ToString()+") "+leaderboard.users[i].username+"#"+leaderboard.users[i].id+"\n<align=right>"+leaderboard.users[i].score.ToString()+"<line-height=1em>\n";
                        if(leaderboard.users[i].id == user.id){
                            user.score = leaderboard.users[i].score;
                        }
                    }
                    return true;
                }
                catch{
                    txtLeaderboard.text = "<color=red>"+webRequest.downloadHandler.text+"</color>";
                    return false;
                }
            }
        ));
    }
    public void SaveHighscore(int score){
        GameObject.Find("GUI/GameOverCanvas/Leaderboard/Viewport/Content/txtLeaderboard").GetComponent<TextMeshProUGUI>().text = "Saving your new highscore!";
        var @params = String.Format("highscore&token={0}&score={1}",user.token,score);
        StartCoroutine(Get(
            String.Format(urltemplate,"account",@params),
            (UnityWebRequest webRequest) => {
                TextMeshProUGUI txtLeaderboard = GameObject.Find("GUI/GameOverCanvas/Leaderboard/Viewport/Content/txtLeaderboard").GetComponent<TextMeshProUGUI>();
                if(webRequest.isNetworkError){
                    txtLeaderboard.text = "<color=red>There was a network error! Please try again.</color>";
                    Debug.Log("Failed to record high score! "+webRequest.error);
                    return false;
                }
                try{
                    Account result = JsonConvert.DeserializeObject<Account>(webRequest.downloadHandler.text);
                    if(result.success){
                        GetLeaderboard();
                        return true;
                    }
                    else{
                        txtLeaderboard.text = "<color=red>"+result.msg+"</color>";
                        Debug.Log("Failed to record high score! "+result.msg);
                        //GetLeaderboard();
                        return false;
                    }
                }
                catch{
                    txtLeaderboard.text = "<color=red>"+webRequest.downloadHandler.text+"</color>";
                    Debug.Log("Failed to record high score! "+webRequest.downloadHandler.text);
                    return false;
                }
            }
        ));
    }
    public void GetMessages(){
        StartCoroutine(Get(
            String.Format(urltemplate,"message",""),
            (UnityWebRequest webRequest) => {
                if(webRequest.isNetworkError){
                    //txtLeaderboard.text = "<color=red>There was a network error! Please try again.</color>";
                    Debug.Log("Failed to fetch chat history! "+webRequest.error);
                    chat.AddLine("<color=red>Failed to fetch chat history!</color>");
                    return false;
                }
                Messages result = JsonConvert.DeserializeObject<Messages>(webRequest.downloadHandler.text);
                if(result.success){
                    Array.Reverse(result.messages);
                    for(int i=0;i<result.messages.Length;i++){
                        if(messages.Count==0 || result.messages[i].id>messages[messages.Count-1].id){
                            chat.AddLine(result.messages[i]);
                            messages.Add(result.messages[i]);
                        }
                    }
                    return true;
                }
                else{
                    chat.AddLine("<color=red>"+result.msg+"</color>");
                    return false;
                }
            }
        ));
    }
    public IEnumerator GetMessagesLoop(){
        while(!string.IsNullOrEmpty(user.token)){
            yield return new WaitForSeconds(1f);
            GetMessages(messages[messages.Count-1].sent);
        }
    }
    public void GetMessages(string since){
        StartCoroutine(Get(
            String.Format(urltemplate,"message","since="+since.Replace(" ","%20")),
            (UnityWebRequest webRequest) => {
                if(webRequest.isNetworkError){
                    //txtLeaderboard.text = "<color=red>There was a network error! Please try again.</color>";
                    Debug.Log("Failed to update chat! "+webRequest.error);
                    chat.AddLine("<color=red>Failed to fetch chat history!</color>");
                    return false;
                }
                Messages result = JsonConvert.DeserializeObject<Messages>(webRequest.downloadHandler.text);
                if(result.success){
                    Array.Reverse(result.messages);
                    for(int i=0;i<result.messages.Length;i++){
                        if(result.messages[i].id>messages[messages.Count-1].id){
                            chat.AddLine(result.messages[i]);
                            messages.Add(result.messages[i]);
                        }
                    }
                    return true;
                }
                else{
                    chat.AddLine("<color=red>"+result.msg+"</color>");
                    return false;
                }
            }
        ));
    }
    public void SendAMessage(string message){
        StartCoroutine(Get(
            String.Format(urltemplate,"message","send&token="+user.token+"&message="+HttpUtility.UrlEncode(message)),
            (UnityWebRequest webRequest) => {
                if(webRequest.isNetworkError){
                    //txtLeaderboard.text = "<color=red>There was a network error! Please try again.</color>";
                    Debug.Log("Failed to send message! "+webRequest.error);
                    chat.AddLine("<color=red>Failed to send your message!</color>");
                    return false;
                }
                Messages result = JsonConvert.DeserializeObject<Messages>(webRequest.downloadHandler.text);
                if(result.success){
                    Array.Reverse(result.messages);
                    for(int i=0;i<result.messages.Length;i++){
                        if(result.messages[i].id>messages[messages.Count-1].id){
                            chat.AddLine(result.messages[i]);
                            messages.Add(result.messages[i]);
                        }
                    }
                    return true;
                }
                else{
                    chat.AddLine("<color=red>"+result.msg+"</color>");
                    return false;
                }
            }
        ));
    }
    public IEnumerator Get(string uri, Func<UnityWebRequest,bool> callback)
    {
        //Debug.Log("Requesting '"+uri+"'");
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();
            
            callback(webRequest);
        }
    }
}

struct Account{
    public bool success;
    public string msg;
    public string action;
    public User user;
}
public struct User{
    public int id;
    public string username;
    public string token;
    public bool loggedin;
    public int score;
}
/*struct UserStatus{
    public User[] users;
}*/
public struct Leaderboard{
    public User[] users;
}
public struct Messages{
    public bool success;
    public string msg;
    public Message[] messages;
}
public struct Message{
    public int id;
    public User author;
    public string content;
    public string sent;
    
}
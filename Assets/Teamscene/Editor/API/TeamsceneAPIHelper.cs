using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEditor;
using UnityEngine;

namespace Teamscene
{
    public class Request
    {
        public string url;
        public string content;
        public IEnumerator routine;
        public bool Finished;
        public event Action OnComplete;

        public Request Next;
        public string Result;

        public void Complete()
        {
            OnComplete?.Invoke();
            Finished = true;
        }

        public void Enqueue()
        {
            TeamsceneAPIRequestHelper.QueueRequest(this);
        }
    }

    [InitializeOnLoad]
    public static class TeamsceneAPIRequestHelper
    {

        private static Queue<Request> currentRequests;
        private static Request current;

        static TeamsceneAPIRequestHelper()
        {
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            currentRequests = new Queue<Request>();
        }

        public static void QueueRequest(Request request)
        {
            currentRequests.Enqueue(request);
        }

        public static Request QueuePOST(string url, string content, Request next = null)
        {
            var r = CreatePOSTRequest(url, content, next);
            currentRequests.Enqueue(r);
            return r;
        }

        public static Request QueueGET(string url, Request next = null)
        {
            var r = CreateGETRequest(url, next);
            currentRequests.Enqueue(r);
            return r;
        }

        public static Request CreateGETRequest(string url, Request next = null)
        {
            Request r = new Request();
            r.url = url;
            r.routine = Get(r);
            r.Next = next;
            return r;
        }

        public static Request CreatePOSTRequest(string url, string content, Request next = null)
        {
            Request r = new Request();
            r.url = url;
            r.content = content;
            r.routine = Post(r);
            r.Next = next;
            return r;
        }

        private static void OnUpdate()
        {
            if (current == null)
            {
                if (currentRequests != null && currentRequests.Count > 0)
                {
                    current = currentRequests.Dequeue();
                }
            }
            else if (current.Finished)
            {
                current = current.Next;
            }
            else
            {
                current.routine.MoveNext();
            }
        }

        private static IEnumerator Get(Request request)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "GET";

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            while (string.IsNullOrEmpty(httpResponse.StatusDescription))
                yield return null;

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                request.Result = streamReader.ReadToEnd();
            }
            // Debug.Log(request.Result);
            request.Complete();
        }

        private static IEnumerator Post(Request request)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(request.url);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(request.content);
            }

            // Debug.Log("JSON: " + request.content);
            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            while (string.IsNullOrEmpty(httpResponse.StatusDescription))
                yield return null;

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                request.Result = streamReader.ReadToEnd();
            }

            request.Complete();
        }
    }
}
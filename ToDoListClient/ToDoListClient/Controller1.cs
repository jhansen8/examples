﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace ToDoListClient
{
    /// <summary>
    /// Controller for the ToDoListClient
    /// </summary>
    public class Controller1
    {
        /// <summary>
        /// The view controlled by this Controller
        /// </summary>
        private IToDoListView view;

        /// <summary>
        /// The token of the most recently registered user, or "0" if no user
        /// has ever registered
        /// </summary>
        private string userToken;

        /// <summary>
        /// The IDs of all the items that are currently displayed, in the order
        /// in which they are displayed
        /// </summary>
        private IList<string> itemList;

        /// <summary>
        /// True if completed tasks are to be displayed, false otherwise
        /// </summary>
        private bool showOnlyCompleted;

        /// <summary>
        /// True if all task are to be displayed, false if only tasks owned
        /// by the most recently registered user are to be displayed
        /// </summary>
        private bool showAll;

        /// <summary>
        /// Creates a Controller for the provided view
        /// </summary>
        public Controller1(IToDoListView view)
        {
            this.view = view;
            userToken = "0";
            itemList = new List<string>();
            showOnlyCompleted = false;
            showAll = false;
            view.CancelPressed += Cancel;
            view.RegisterPressed += Register;
            view.SubmitPressed += Submit;
            view.DeletePressed += Delete;
            view.DonePressed += Done;
            view.FilterChanged += Filter;
        }

        /// <summary>
        /// Cancels the current operation (currently unimplemented)
        /// </summary>
        private void Cancel()
        {
        }

        /// <summary>
        /// Registers a user with the given name and email.
        /// </summary>
        private void Register(string name, string email)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    // Create the parameter
                    dynamic user = new ExpandoObject();
                    user.Name = name;
                    user.Email = email;

                    // Compose and send the request.
                    StringContent content = new StringContent(JsonConvert.SerializeObject(user), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync("RegisterUser", content).Result;

                    // Deal with the response
                    if (response.IsSuccessStatusCode)
                    {
                        String result = response.Content.ReadAsStringAsync().Result;
                        userToken = (string)JsonConvert.DeserializeObject(result);
                        view.IsUserRegistered = true;
                    }
                    else
                    {
                        MessageBox.Show("Error registering: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Submits a new task to the ToDo list.  A description of the task
        /// must be provide.
        /// </summary>
        private void Submit(string description)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    // Create the parameter
                    dynamic task = new ExpandoObject();
                    task.UserID = userToken;
                    task.Description = description;

                    // Compose and send the request.
                    StringContent content = new StringContent(JsonConvert.SerializeObject(task), Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync("AddItem", content).Result;

                    // Deal with the response
                    if (response.IsSuccessStatusCode)
                    {
                        String result = response.Content.ReadAsStringAsync().Result;
                        dynamic itemToken = JsonConvert.DeserializeObject(result);
                    }
                    else
                    {
                        MessageBox.Show("Error submitting: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
                Refresh();
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Deletes the task at the specified index of the display.
        /// </summary>
        private void Delete(int index)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    // Compose and send the request
                    String url = String.Format("DeleteItem/{0}", itemList[index]);
                    HttpResponseMessage response = client.DeleteAsync(url).Result;

                    // Deal with the response
                    if (response.IsSuccessStatusCode)
                    {
                        Refresh();
                    }
                    else
                    {
                        MessageBox.Show("Error deleting: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Marks as done the task at the specified index of the display.
        /// </summary>
        private void Done(int index)
        {
            try
            {
                view.EnableControls(false);
                using (HttpClient client = CreateClient())
                {
                    // Compose and send the request
                    String url = String.Format("MarkCompleted/{0}", itemList[index]);
                    StringContent content = null;
                    HttpResponseMessage response = client.PutAsync(url, content).Result;

                    // Deal with the response
                    if (response.IsSuccessStatusCode)
                    {
                        Refresh();
                    }
                    else
                    {
                        MessageBox.Show("Error marking as done: " + response.StatusCode + "\n" + response.ReasonPhrase);
                    }
                }
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Changes the state of the filter that control what is to be displayed.
        /// </summary>
        private void Filter(bool showAll, bool showOnlyCompleted)
        {
            try
            {
                view.EnableControls(false);
                this.showAll = showAll;
                this.showOnlyCompleted = showOnlyCompleted;
                Refresh();
            }
            finally
            {
                view.EnableControls(true);
            }
        }

        /// <summary>
        /// Refreshes the display because something has changed.
        /// </summary>
        private void Refresh()
        {
            using (HttpClient client = CreateClient())
            {
                // Compose and send the request
                String url;
                if (showAll)
                {
                    url = String.Format("GetAllItems?completed={0}", showOnlyCompleted);
                }
                else
                {
                    url = String.Format("GetAllItems?completed={0}&user={1}", showOnlyCompleted, userToken);
                }
                HttpResponseMessage response = client.GetAsync(url).Result;

                // Deal with the response
                if (response.IsSuccessStatusCode)
                {
                    String result = response.Content.ReadAsStringAsync().Result;
                    dynamic items = JsonConvert.DeserializeObject(result);
                    view.Clear();
                    itemList.Clear();
                    foreach (dynamic item in items)
                    {
                        view.AddItem((string)item.Description, (bool)item.Completed, item.UserID == userToken);
                        itemList.Add((string)item.ItemID);
                    }
                }
                else
                {
                    MessageBox.Show("Error getting items: " + response.StatusCode + "\n" + response.ReasonPhrase);
                }
            }
        }

        /// <summary>
        /// Creates an HttpClient for communicating with the server.
        /// </summary>
        private static HttpClient CreateClient()
        {
            // Create a client whose base address is the GitHub server
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:44444/ToDo.svc/");

            // Tell the server that the client will accept this particular type of response data
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Add("Accept", "application/json");

            // There is more client configuration to do, depending on the request.
            return client;
        }
    }
}

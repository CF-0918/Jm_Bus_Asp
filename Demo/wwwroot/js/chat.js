"use strict";

var connection = new signalR.HubConnectionBuilder().withUrl("/chatHub").build();

// Disable the send button until connection is established.
document.getElementById("sendButton").disabled = true;

connection.on("ReceiveMessage", function (user, message) {
    var currentUser = document.getElementById("userInput").value;
    var li = document.createElement("li");
    li.className = user === currentUser ? "user-message" : "other-message";
    li.textContent = `${user} : ${message}`;
    document.getElementById("messagesList").appendChild(li);
    var chatBox = document.getElementById("chatBox");
    chatBox.scrollTop = chatBox.scrollHeight; // Auto-scroll to the latest message
});

connection.start().then(function () {
    document.getElementById("sendButton").disabled = false;
}).catch(function (err) {
    return console.error(err.toString());
});

document.getElementById("sendButton").addEventListener("click", function (event) {
    var user = document.getElementById("userInput").value.trim();
    var message = document.getElementById("messageInput").value.trim();

    if (user && message) {
        connection.invoke("SendMessage", user, message).catch(function (err) {
            return console.error(err.toString());
        });
        document.getElementById("messageInput").value = ""; // Clear input field
    }
    event.preventDefault();
});

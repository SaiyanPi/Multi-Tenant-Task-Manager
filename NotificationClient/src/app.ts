import * as signalR from "@microsoft/signalr";
    

// Prompt for userId and token (or hardcode for now)
// const userId = prompt("Enter your User ID:") || "";
const token = prompt("Enter your JWT token");
// const tenantId = prompt("Enter your Tenant ID:");

const connection = new signalR.HubConnectionBuilder()
    .withUrl("https://localhost:7194/hubs/notifications", {
      accessTokenFactory: () => token
    })
    .configureLogging(signalR.LogLevel.Information)
    .build();
    

connection.on("ReceiveNotification", (notification: any) => {
  const ul = document.getElementById("notifications")!;
  const li = document.createElement("li");
  li.textContent = `[${new Date(notification.createdAt).toLocaleTimeString()}] ${notification.title}: ${notification.message}`;
  ul.prepend(li);
});

connection
  .start()
  .then(() => console.log("Connected to SignalR hub"))
  .catch(err => console.error("Error connecting to SignalR:", err));
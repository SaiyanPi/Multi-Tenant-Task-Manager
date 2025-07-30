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

  const maxLength = 50;
  let preview = notification.content || "";
  if (preview.length > maxLength) {
      preview = preview.substring(0, maxLength) + "...";
  }
  // Set the element ID for later lookup
  li.id = `notification-${notification.id}`;
  
  // li.textContent = `[${new Date(notification.createdAt).toLocaleTimeString()}] ${notification.title}: ${notification.message}`;
  const type = (notification.type || "general").toLowerCase();

  console.log("Notification Type:", type);

  if (type === "comment") {
    li.innerHTML = `💬 New comment on <strong>${notification.projectName || notification.taskName}</strong> by <em>${notification.senderName}</em>:<br>${preview}`;
  } else {
    li.textContent = `[${new Date(notification.createdAt).toLocaleTimeString()}] ${notification.title}: ${notification.message}`;
  }

  console.log("Received Notification:", notification);

  // Add "Mark as Read" button
  const button = document.createElement("button");
  button.textContent = "Mark as Read";
  button.onclick = () => {
    connection.invoke("MarkAsRead", notification.id)
    .then(() => {
        // Visually mark as read
        li.classList.add("read");
        li.style.opacity = "0.5";
        li.style.textDecoration = "line-through";
      
        // Remove the button to prevent duplicate clicks
        button.remove();
      })
      .catch(err => console.error("Error marking as read:", err));
      
      console.log(`Notification ID: ${notification.id} `);
  };
 


  li.appendChild(button);
  ul.prepend(li);
});

// Handle real-time IsRead update
connection.on("NotificationReadUpdated", (update: any) => {
  console.log(`Notification ${update.NotificationId} marked as read in real-time.`);

  const li = document.getElementById(`notification-${update.NotificationId}`);
  if (li) {
    li.classList.add("read");
    li.style.opacity = "0.5";
    li.style.textDecoration = "line-through";
  }
});

// Start the connection
connection
  .start()
  .then(() => console.log("Connected to SignalR hub"))
  .catch(err => console.error("Error connecting to SignalR:", err));
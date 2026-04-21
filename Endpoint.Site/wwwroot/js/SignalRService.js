var chatBox = $("#ChatBox");

var connection = new signalR.HubConnectionBuilder()
    .withUrl("/chathub")
    .build();

connection.start();


//connection.invoke('SendNewMessage', "بازدید کننده", "سلام این پیام از سمت کلاینت ارسال شده است");

  //نمایش چت باکس برای کاربر
function showChatDialog() {
    chatBox.css("display", "block");
}

function Init() {
    setTimeout(showChatDialog, 1000);
}
$(document).ready(function () {
    Init();
});

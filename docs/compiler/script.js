var darkmode = window.matchMedia('(prefers-color-scheme: dark)').matches;
var sideBarOn = false;

$(function() {
  $("*").css("transition", "background-color 0.3s, color 0.1s");

  window
	.matchMedia("(prefers-color-scheme: dark)")
	.addEventListener("change", function (e) {
		darkmode = e.matches;
      viewMode();
	});

  viewMode();
});

function toogleSideMenu() {
  console.log(sideBarOn);
  if (sideBarOn) {
    $("#side-menu").animate({
    right: "100%",
    left: "-80%"
}, 1000);
    sideBarOn = false;
  } else {
    $("#side-menu").animate({
    right: "20%",
    left: "0%"
}, 750);
    sideBarOn = true;
  }
}

function viewMode() {
  if (darkmode) {
    $("body").css("background-color", "#444");
    $(".box").css("background-color", "#333");
    $("body").css("color", "#eee");
    $("#mode-box").css("background-color", "#333");
    $("button.enabled").css("background-color", "#111");
    $("button.enabled").css("color", "#eee");
    $("button.not-enabled").css("background-color", "#3f3f3f");
    $("button.not-enabled").css("color", "#666");
    $("#console").css("color", "white");
    $("#console").css("background-color", "black");
    $("#menu-bar").css("background-color", "#111");
    $(".menu-icon").css("background-color", "#eee");
    $("#side-menu").css("background-color", "#222");
    $(".menu-item").css("background-color", "#333");

  } else {
    $("body").css("background-color", "#fafafa");
    $(".box").css("background-color", "#eee");
    $("body").css("color", "#000");
    $("#mode-box").css("background-color", "#eee");
    $("button.enabled").css("background-color", "#777");
    $("button.enabled").css("color", "#000");
    $("button.not-enabled").css("background-color", "#ddd");
    $("button.not-enabled").css("color", "#aaa");
    $("#console").css("color", "black");
    $("#console").css("background-color", "white");
    $("#manu-bar").css("background-color", "#fff");
    $(".menu-icon").css("background-color", "#555");
    $("#side-menu").css("background-color", "#ddd");
    $(".menu-item").css("background-color", "#eee");
  }
}

$("#mode").on("click", function() {
  darkmode = !darkmode;
  viewMode();
});


console.log('S# init...');

var output;

function getInput() {
  return $("#equation").val();
}

function print(text) {
  output += text;
  $("#output").text(output);
}

function clear() {
  output = "";
  print("");
}

function run() {
  clear();
  print(getInput());
}
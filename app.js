$(function () {
    //Enable swiping...
    $("body").swipe({
        swipeLeft: function (event, direction, distance, duration, fingerCount) {
            alert(1);
        },
        swipeRight: function (event, direction, distance, duration, fingerCount) {
            alert(2);
        },
        //Default is 75px, set to 0 for demo so any distance triggers swipe
        threshold: 75
    });
});
var SpotifyLogin;
(function (SpotifyLogin) {
    var CEvent = CustomEvents.CEvent;
    var localToken = localStorage.getItem("token");
    function login(callback, forced) {
        if (!forced && localToken) {
            callback(localToken);
            return;
        }
        else if (forced) {
            localToken = null;
            localStorage.removeItem("token");
        }
        var clientId = "26e3d7571b7947fd9080aa94c2c0621d";
        function getLoginURL(scopes) {
            return "https://accounts.spotify.com/authorize"
                + ("?client_id=" + clientId)
                + ("&redirect_uri=" + encodeURIComponent("http://das.party/auth.aspx"))
                + ("&scope=" + encodeURIComponent(scopes.join(" ")))
                + "&response_type=token";
        }
        var url = getLoginURL(["user-read-email"]);
        var width = 450, height = 730, left = (screen.width / 2) - (width / 2), top = (screen.height / 2) - (height / 2);
        window.addEventListener("message", function (event) {
            var hash = JSON.parse((event.data));
            if (hash.type === "access_token") {
                localStorage.setItem("token", hash.access_token);
                callback(hash.access_token);
            }
        }, false);
        window.open(url, "Spotify", "menubar=no,location=no,resizable=no,scrollbars=no,status=no," +
            (" width=" + width + ", height=" + height + ", top=" + top + ", left=" + left));
    }
    function getUserData(accessToken) {
        return $.ajax({
            url: "https://api.spotify.com/v1/me",
            headers: {
                'Authorization': "Bearer " + accessToken
            }
        });
    }
    var loginEvent = new CEvent();
    function onLogin(handler) {
        if (SpotifyLogin.loggedInUser)
            handler();
        else
            loginEvent.addHandler(handler);
    }
    SpotifyLogin.onLogin = onLogin;
    function startParty(user) {
        SpotifyLogin.loggedInUser = user;
        loginEvent.trigger();
        $("#account-info").html("Hello" + (user.display_name ? ", " + user.display_name
            : ($.isNumeric(user.id) ? " there" : ", " + user.id))
            + "! Let's party");
        $("#login-container").hide();
        $("#playlist-wrapper").show();
    }
    function tryLogin(forced) {
        login(function (accessToken) {
            getUserData(accessToken).then(function (response) {
                console.log(response);
                startParty(response);
            });
        }, forced);
    }
    if (localToken)
        tryLogin();
    $("#btn-login").click(function () {
        if (location.hostname === "localhost" || location.hostname === "127.0.0.1") {
            startParty({
                display_name: "Test",
                email: "test@test.com",
                external_urls: {
                    spotify: "https://open.spotify.com/user/test"
                },
                followers: {
                    href: null,
                    total: 10
                },
                href: "https://api.spotify.com/v1/users/test",
                id: "testaccount",
                images: [
                    {
                        height: null,
                        url: "https://scontent.xx.fbcdn.net/v/t1.0-1/p200x200/11542099_796414300457857_4421964208460940101_n.jpg?oh=9c7d2af153cb59848b9f3e50e159c8d2&oe=58C83D80",
                        width: null
                    }
                ],
                type: "user",
                uri: "spotify:user:test"
            });
        }
        else {
            tryLogin(typeof localToken !== "undefined" && localToken !== null);
        }
    });
})(SpotifyLogin || (SpotifyLogin = {}));
//# sourceMappingURL=login.js.map
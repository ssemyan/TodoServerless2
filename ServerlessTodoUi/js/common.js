// Global Namespace
var AZ = {};

// Global Vars
var accessToken = "";
var tokenExpire;

AZ.Ajax = (function () {
    "use strict";

    $(document).ready(function () {
        // Add please wait to body and attach to ajax function
        var loadingDiv = "<div id='ajax_loader' style='width: 100%;height: 100%;top: 0;left: 0;position: fixed;opacity: 0.7;background-color: #fff;z-index: 9999;text-align: center;display: none;'><h1 style='margin-top: 300px;'>Loading...</h1></div>";
        $("body").append(loadingDiv);

        $(document).ajaxStart(function () {
            $("#ajax_loader").show();
        });

        $(document).ajaxComplete(function (event, jqxhr, settings) {
            if (settings.url.startsWith("/.auth/")) return; // Keep loading div open on auth requests
            $("#ajax_loader").hide();
        });
    });

    // Errors just get an alert
    function handleBasicError(xhr, status, error) {
        alert("Error: " + error);
    }

    // Ajax call
    function makeAjaxCall(ajaxType, ajaxUrl, data, successFunc) {

        // If we have an access token add it to the request as a Basic Auth Header (on localhost we will not have a token)
        var header = {};
        if (accessToken) {
            header = { "Authorization": 'Bearer ' + accessToken };
        }

        $.ajax({
            type: ajaxType,
            url: ajaxUrl,
            data: data,
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            success: successFunc,
            error: handleBasicError,
            headers: header
        });
    }

    // Perform a login and set the access token and expire
    function doLogin(successFunc, errorFunc) {

        // Determine if we are logged in or not
        $.ajax({
            type: "GET",
            url: '/.auth/me',
            success: function (response) {
                //console.log(response); // Show Claims
                accessToken = response[0].access_token;
                tokenExpire = new Date(response[0].expires_on);
                successFunc(response);
            },
            error: errorFunc
        });
    }

    return {

        MakeAjaxCall: function (ajaxType, ajaxUrl, data, successFunc) {

            // If we have an access token make sure it is not expired
            if (accessToken) {
                var now = new Date();
                console.log("checking auth token expire. Current time " + now.toString() + " tokenExpire " + tokenExpire.toString());
                if (tokenExpire < now) {
                    console.log("refreshing auth token");
                    $.ajax({
                        type: "GET",
                        url: '/.auth/refresh',
                        success: function () {
                            doLogin(function () {
                                makeAjaxCall(ajaxType, ajaxUrl, data, successFunc);
                            },
                                function () {
                                    alert("Error getting new auth token");
                                    $("#ajax_loader").hide(); // Manually hide the loading message
                                });
                        },
                        error: function () {
                            alert("Error refreshing auth token");
                            $("#ajax_loader").hide(); // Manually hide the loading message
                        }
                    });
                } else {
                    // Token not expired
                    makeAjaxCall(ajaxType, ajaxUrl, data, successFunc);
                }
            } else {
                // No token so must be on localhost
                makeAjaxCall(ajaxType, ajaxUrl, data, successFunc);
            }
        },

        DoLogin: function (successFunc, errorFunc) {
            doLogin(successFunc, errorFunc);
        }
    };
}());

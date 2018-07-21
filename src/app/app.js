'use strict';

var app = angular.module('LightshowApp', []);

document.addEventListener('DOMContentLoaded', function () {
    angular.bootstrap(document, ['LightshowApp']);
});

app.controller('ChainCtrl', function (ChainService) {
    var ctrl = this;
    ctrl.Title = 'Lightshow Studio Chain // Lightweight';

    LoadContacts();

    function LoadContacts() {
        ChainService.Get()
            .then(function (chain) {
                ctrl.Chain = chain
            }, function (error) {
                ctrl.ErrorMessage = error
            });
    }
});

app.service('ChainService', function ($http) {
    var svc = this;
    var apiUrl = 'http://localhost:5000/api';

    svc.Get = function () {
        return $http.get(apiUrl + '/set')
            .then(function success(response) {
                return response.data;
            });
    }
});
// Copyright 2014 Murray Grant
//
//    Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


// A global argument to the password get event (below);
window.pwg = window.pwg || {};

(function (ns, undefined) {
    // A list of human readable ratings for combinations.
    var m_ratings = [
        { title: "Unusable", description: "Too few combinations to be used in any circumstances. You should increase length or complexity parameters substantially." },
        { title: "Inadequate", description: "Generated passwords are of very poor quality. They should only be used for testing." },
        { title: "Passable", description: "Generated passwords are of a minimal level of quality. They should only be used for extremely low value accounts." },
        { title: "Adequate", description: "Generated passwords are usable for most purposes including low value website accounts and computer logins. They should not be used for any financial accounts." },
        { title: "Strong", description: "Generated passwords are above average quality. They can be used to protect medium value web accounts, computer logins and financial information." },
        { title: "Fantastic", description: "Generated passwords are well above average! Use them to protect high value accounts like Facebook and Google, financial information and for disk encryption." },
        { title: "Unbreakable", description: "Passwords generated by these parameters are impossible to guess, even if every computer on the planet was dedicated to the task!" },
        { title: "Overkill", description: "Passwords of this complexity yield no additional value. You are commended for using such a password, but it's really a waste! (And yes, this is the highest rating this site will give)." }
    ];

    // The front page maps simple options to actual parameters.
    var m_frontLookup = {
        readable: {
            short: { s: 'RandomShort', pc: 1, sp: 'y', },
            long: { s: 'RandomLong', pc: 1, sp: 'y', },
            forever: { s: 'RandomForever', pc: 1, sp: 'y', },
            inputSelector: '#pwTypeReadable'
        },
        dict: {
            short: { wc: 4, pc: 1, sp: 'y', },
            long: { wc: 5, pc: 1, sp: 'y', },
            forever: { wc: 7, pc: 1, sp: 'y', },
            inputSelector: '#pwTypeDict'
        }
    };

    // These are mapped onto the object after the main front parameters above.
    var m_frontExtras = {
        nums: {
            whenNum: 'EndOfWord',
            nums: '2',
        },
        caps: {
            whenUp: 'StartOfWord',
            ups: '2',
        },
        capsWord: {
            whenUp: 'WholeWord',
            ups: '1',
        },
        wifi: {
            sp: 'n',
            whenUp: 'StartOfWord',
            ups: '999',
            maxCh: 63,
        },
    };

    // Mutators are a lookup from radio buttons to actual parameters.
    var m_mutatorLookup = {
        None: {
            whenNum: 'Never',
            nums: '0',
            whenUp: 'Never',
            ups: '0',
        },
        Standard: {
            whenNum: 'EndOfWord',
            nums: '2',
            whenUp: 'StartOfWord',
            ups: '2',
        },
        Word: {
            whenNum: 'EndOfWord',
            nums: '2',
            whenUp: 'WholeWord',
            ups: '1',
        },
        Upper: {
            whenNum: 'Never',
            nums: '0',
            whenUp: 'StartOfWord',
            ups: '999',
        },
        UpperAndNumber: {
            whenNum: 'EndOfWord',
            nums: '2',
            whenUp: 'StartOfWord',
            ups: '999',
        },
    };

    // When this reaches zero, a warning is displayed about ip based limits.
    ns.ipLimitWarningCounter = 10;

    // A page can use this to validate parameters.
    ns.validation = function () { return true; };

    // Front page logic.
    ns.doFrontPassword = function () {
        // Lookup the parameters.
        var frontParms = $('#passwordParameters input').serializeObject();
        if (!frontParms.pwType) {
            // If nothing was selected, randomly choose something just so we get a password.
            var keys = Object.keys(m_frontLookup);
            frontParms.pwType = keys[keys.length * Math.random() << 0];         // http://stackoverflow.com/a/15106541
            $('#passwordParameters input[name="pwType"][value="' + frontParms.pwType + '"]').prop('checked', true).trigger('change');
        }
        var strengthLookup = m_frontLookup[frontParms.pwType];
        var parms = {};     // Have to make a copy of this or we end up with nasty reference problems between calls.
        if (!frontParms.pwStrength) {
            // If nothing was selected, use the 'short' option.
            frontParms.pwStrength = 'short';
            $('#passwordParameters input[name="pwStrength"][value="short"]').prop('checked', true).trigger('change');
        }
        var original = strengthLookup[frontParms.pwStrength];
        for (var k in original) {
            parms[k] = original[k];
        }
        // Map extras onto the parameters.
        var extras = $('#passwordParameters input[name="extras"]:checked');
        extras.each(function () {
            var $extra = $(this);
            var extra = m_frontExtras[$extra.val()];
            for (var k in extra) {
                parms[k] = extra[k];
            }
        });

        // Lookup the urls.
        var passwordUrl = $(strengthLookup.inputSelector).data('urlpassword')

        // Run the normal get password logic, but pass parameters in.
        ns.getPassword(passwordUrl, parms, function () {
            $('#passwordPostFront').show();
            $('#impatientPasswordHint').show();
        });
    };

    // The main logic.
    ns.getPassword = function (url, parms, onSuccess) {

        // TODO: validation.
        if (!parms) {
            var parms = getParameters();
        }
        doIpLimitWarning();

        $('.password-get').button('loading');
        $('#passwordPlace').html('<span class="pw">Generating...</span>');
        $.getJSON(url, parms, function (d, status) {
            // TODO: the string.charCodeAt() function returns unicode values in the BMP, which we could use to allow display of code points in hex.
            // TODO: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/String/charCodeAt has some examples of handling code points outside the BMP.
            // TODO: suggest a (double)click on a password to change to code points as U+1234.

            var html = '';
            $.each(d.pws, function (i, x) {
                html = html + '<span class="pw">' + x + '</span><br />';
            });
            if (html.length > 1) {
                html.substr(0, html.length - 6);
            }
            $('#passwordPlace').html(html);
            $('#passwordPlace').show();
            $('#passwordError').hide();
            $('.password-get').button('reset');
            $('#passwordPostFront').show();

            // Callback.
            if (onSuccess) {
                onSuccess();
            }
        }).error(function (e) {
            $('.password-get').button('reset');
            if (e.status == 403) {
                $('#passwordError .message').text(e.statusText);
            } else {
                $('#passwordError .message').text("Sorry, we couldn't get your password because of a server error.");
            }
            $('#passwordError').show();
            $('#passwordPlace').hide();
            $('#passwordPostFront').hide();
        });
    };

    ns.getCombinations = function (url) {
        // TODO: validation.
        var parms = getParameters();
        doIpLimitWarning();

        $.getJSON(url, parms, function (d, status) {
            // TODO: error handling.

            if (d.upper && d.lower && d.middle) {
                // This response is a range.
                var lowerRating = m_ratings[d.lower.rating];
                var upperRating = m_ratings[d.upper.rating];
                var middleRating = m_ratings[d.middle.rating];

                // Poor combinations get red / yellow colours.
                $('#combinationsPlace ').removeClass('panel-default').removeClass('panel-danger').removeClass('panel-warning');
                if (d.middle.rating <= 1) 
                    $('#combinationsPlace').addClass('panel-danger');
                else if (d.middle.rating == 2)
                    $('#combinationsPlace').addClass('panel-warning');
                else 
                    $('#combinationsPlace').addClass('panel-default');

                var headline = middleRating.title + ' <small>(Ranges between ' + lowerRating.title + ' and ' + upperRating.title + ')</small>';
                $('#combinationsPlace .rating > h4').html(headline);
                $('#combinationsPlace .rating > p').text(middleRating.description);

                $('#combinationsPlace .fineprint .middle span.formatted').text(d.middle.formatted);
                $('#combinationsPlace .fineprint .middle span.base2').text(d.middle.base2);
                $('#combinationsPlace .fineprint .middle span.base10').text(d.middle.base10);

                $('#combinationsPlace .fineprint .lower span.formatted').text(d.lower.formatted);
                $('#combinationsPlace .fineprint .lower span.base2').text(d.lower.base2);
                $('#combinationsPlace .fineprint .lower span.base10').text(d.lower.base10);

                $('#combinationsPlace .fineprint .upper span.formatted').text(d.upper.formatted);
                $('#combinationsPlace .fineprint .upper span.base2').text(d.upper.base2);
                $('#combinationsPlace .fineprint .upper span.base10').text(d.upper.base10);

            } else {
                // Assume only a single number.
                var rating = m_ratings[d.rating];

                // Poor combinations get red / yellow colours.
                $('#combinationsPlace ').removeClass('panel-default').removeClass('panel-danger').removeClass('panel-warning');
                if (d.rating <= 1)
                    $('#combinationsPlace').addClass('panel-danger');
                else if (d.rating == 2)
                    $('#combinationsPlace').addClass('panel-warning');
                else
                    $('#combinationsPlace').addClass('panel-default');

                $('#combinationsPlace .rating > h4').html(rating.title);
                $('#combinationsPlace .rating > p').text(rating.description);

                $('#combinationsPlace .fineprint span.formatted').text(d.formatted);
                $('#combinationsPlace .fineprint span.base2').text(d.base2);
                $('#combinationsPlace .fineprint span.base10').text(d.base10);
            }

            $('#combinationsPlace').show();
        }).error(function () {
            $('#combinationsPlace').hide();
        });
    };

    function getParameters() {
        // This only includes checked tickboxes, we have to manually add unchecked ones.
        var result = $('#passwordParameters input').serialize();
        $('#passwordParameters input[type="checkbox"]:not(:checked)')
            .each(function (idx, el) {
                // This assumes there's already something in the result.
                result = result + '&' + $(this).attr('name') + '=' + $(this).data('uncheckedvalue');
            });
        return result;
    }

    function doIpLimitWarning() {
        ns.ipLimitWarningCounter = ns.ipLimitWarningCounter - 1;
        if (ns.ipLimitWarningCounter <= 0) {
            $('#ipBasedLimitWarning').show(1000);
        }
    }

    ns.applyPrefs = function (prefs) {
        for (var key in prefs) {
            var input = $('#passwordParameters [name="' + key + '"]');
            if (input.length === 1)
                input.val(prefs[key]);
            else if (input.length > 1) {
                // Probably a bunch of radio buttons / checkboxes.
                input.prop('checked', false);
                input.filter('[value="' + prefs[key] + '"]').prop('checked', true);
            }

        }
    }

    ns.getMutatorParams = function (key) {
        if (key in m_mutatorLookup)
            return m_mutatorLookup[key];
        else
            return {};
    }
}(window.pwg));


// Password button click handler.
$(document).on('click', '.password-get', function (evt) {
    if ($(evt.target).attr('id') === 'passwordGetFront') {
        // The front page needs to run through a lookup table.
        window.pwg.doFrontPassword();
    } else {
        // Specific pages hit the JSON api directly.
        var url = $(evt.target).data('urlpassword');
        window.pwg.getPassword(url);
    }
});


// Remember / restore the user's preferences on each page in local storage.
$(function () {
    // Reload on page load.
    if (!localStorage)
        return;

    var key = $('#passwordParameters').data('pagekey');
    if (key) {
        var raw = localStorage[key + '_prefs'];
        if (raw) {
            var prefs = JSON.parse(raw);
            pwg.applyPrefs(prefs);
        }
    }
});
$(document).on('change', '#passwordParameters input', function (evt) {
    // Remember changes in local storage.
    if (!localStorage)
        return;

    var key = $('#passwordParameters').data('pagekey');
    var data = JSON.stringify($('#passwordParameters input').serializeObject());
    if (key && data) {
        localStorage[key + '_prefs'] = data;
    }
});

$(document).ready(function () {
    // Automatically trigger getting combinations and a default password on each of the specific pages.
    var getBtn = $('#passwordGet');
    if (getBtn.length) {
        var combinationsUrl = $('#passwordGet').data('urlcombinations');
        var passwordUrl = $('#passwordGet').data('urlpassword');

        // Hook on blur / change events for textboxes and checkboxes.
        $(document).on('blur', '#passwordParameters input[type="number"]', function (evt) {
            window.pwg.getCombinations(combinationsUrl);
        });
        $(document).on('change', '#passwordParameters input[type="checkbox"], #passwordParameters input[type="radio"]', function (evt) {
            var url = $('#passwordGet').data('urlcombinations');
            window.pwg.getCombinations(combinationsUrl);
        });


        window.pwg.getCombinations(combinationsUrl);
        window.pwg.getPassword(passwordUrl);
    }

    // If the front page has a #getPassword, automatically get a password.
    if (window.location.hash && window.location.hash.toLowerCase() === '#getpassword') {
        getBtn = $('#passwordGetFront');
        if (getBtn.length) {
            $('#impatientPassword').trigger('click');
        }
    }
});

// The Generate Password Right Now Button (front page).
$(document).on('click', '#impatientPassword', function (evt) {
    evt.preventDefault();

    // Previous prefs are stored in local storage and should be set on page load.
    // But, if something went wrong, or they've never been here before, choose some stuff at random.

    if (!$('input[name="pwType"]:checked').length) {
        // Randomly choose a style.
        var rand = Math.floor(Math.random() * 2) + 1;
        if (rand > 0 && rand <= 1) {
            $('#pwTypeReadable').prop('checked', true).trigger('change');
        } else if (rand > 1 && rand <= 2) {
            $('#pwTypeDict').prop('checked', true).trigger('change');
        }
    }
    if (!$('input[name="pwStrength"]:checked').length) {
        // Always select the shortest strength.
        $('#pwPredefinedStrengthShort').prop('checked', true).trigger('change');
    }


    window.location = "#getpassword";
    $('#passwordGetFront').trigger('click');
});

// Show details about why a range for readable passphrases.
$(document).on('click', '#readableWhyRangeBtn', function (evt) {
    $('#readableWhyRangeBtn').hide();
    $('#readableWhyRangeText').show(400);
});


// The mutators are a special case, they map to some hidden inputs.
$(document).on('change', '.parameters-panel .mutators input[type="radio"]', function (evt) {
    var val = $(this).val();
    var params = pwg.getMutatorParams(val);
    for (var key in params) {
        $('.parameters-panel .mutators input[type="hidden"][name="' + key + '"]').val(params[key]).trigger('change');
    }
});


// Detect the old domain and show the moving alert.
$(document).ready(function () {
    if (window.location.hostname.toLowerCase().indexOf('makemeapassword.org') !== -1) {
        $('#domainChangeAlert').show(400);
    }
});

// http://stackoverflow.com/a/1186309/117070
$.fn.serializeObject = function () {
    var o = {};
    var a = this.serializeArray();
    $.each(a, function () {
        if (o[this.name] !== undefined) {
            if (!o[this.name].push) {
                o[this.name] = [o[this.name]];
            }
            o[this.name].push(this.value || '');
        } else {
            o[this.name] = this.value || '';
        }
    });
    return o;
};


// Alert dialog dismiss bvutton.
$(".alert").alert();


// Hide the http warning if we're on https.
// This can sometimes be displayed if the page is cached.
if (window.location.protocol.toLowerCase() === 'https:')
    $("#noHttpsWarning").hide();

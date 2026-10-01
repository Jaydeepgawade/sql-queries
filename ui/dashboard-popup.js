// Include after your MVC application's existing jQuery script.
// Call equityDashboard.load(filters, accessToken) using a token from your login flow.
// Use a same-origin server proxy when your MVC authentication uses a session/cookie.
(function ($) {
    let currentRequest;
    let requestId = 0;

    function popup(message) {
        const dialog = document.getElementById('dashboardMessageDialog');
        document.getElementById('dashboardMessageText').textContent = message;
        if (!dialog.open) dialog.showModal();
    }

    window.equityDashboard = {
        load: function (filters, accessToken) {
            const id = ++requestId;
            if (currentRequest) currentRequest.abort();
            const dialog = document.getElementById('dashboardMessageDialog');
            if (dialog.open) dialog.close();
            // Clear stale values so a missing quarter never shows the previous quarter's data.
            $('#dashboardResults').empty();
            $('#dashboardLoading').prop('hidden', false);
            $('#dashboardLoadButton').prop('disabled', true);
            currentRequest = $.ajax({
                url: '/api/equity-dashboard',
                method: 'GET',
                data: filters,
                dataType: 'json',
                headers: { Authorization: 'Bearer ' + accessToken }
            }).done(function (response) {
                if (id !== requestId) return;
                if (!response.hasData) {
                    if (response.showPopup) {
                        const message = response.code === 'QUARTER_DATA_NOT_FOUND'
                            ? 'निवडलेल्या quarter साठी तुमच्या role मध्ये dashboard data उपलब्ध नाही.'
                            : 'निवडलेल्या RM, position किंवा status filters साठी records उपलब्ध नाहीत.';
                        popup(message);
                    }
                    return;
                }
                // Safe text rendering. Replace this area with your chart/grid bindings.
                const totals = response.data.totals;
                $('<p>').text('Gross Incentive: ' + totals.grossIncentive).appendTo('#dashboardResults');
                $('<p>').text('Adjustment: ' + totals.adjustmentAmt).appendTo('#dashboardResults');
                $('<p>').text('Net Incentive: ' + totals.netIncentive).appendTo('#dashboardResults');
                $('<p>').text('Incentive records: ' + totals.incentiveRecordCount).appendTo('#dashboardResults');
                response.data.statusCards.forEach(function (card) {
                    $('<p>').text(card.statusName + ': ' + card.rmQuarterCount + ' RM-quarter pairs')
                        .appendTo('#dashboardResults');
                });
                // Other consumers can bind Chart.js and grid from this event.
                $(document).trigger('equityDashboard:loaded', [response.data]);
            }).fail(function (xhr, state) {
                if (id !== requestId || state === 'abort') return;
                if (xhr.status === 401) popup('Session/token expired. Please sign in again.');
                else if (xhr.status === 403) popup('You do not have permission to view this dashboard.');
                else if (xhr.status === 400) popup(xhr.responseJSON?.detail || 'Please check the selected filters.');
                else popup('Unable to load dashboard data. Please try again.');
            }).always(function () {
                if (id !== requestId) return;
                $('#dashboardLoading').prop('hidden', true);
                $('#dashboardLoadButton').prop('disabled', false);
                currentRequest = null;
            });
            return currentRequest;
        }
    };
})(jQuery);

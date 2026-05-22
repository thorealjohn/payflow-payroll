(function () {
    function digitsOnly(v) { return v.replace(/\D/g, ''); }

    function formatTIN(d) {
        var r = '';
        for (var i = 0; i < d.length; i++) {
            if (i === 3 || i === 6 || i === 9) r += '-';
            r += d[i];
        }
        return r;
    }

    function formatSSS(d) {
        var r = '';
        for (var i = 0; i < d.length; i++) {
            if (i === 2) r += '-';
            if (i === 9) r += '-';
            r += d[i];
        }
        return r;
    }

    function formatPhilHealth(d) {
        var r = '';
        for (var i = 0; i < d.length; i++) {
            if (i === 2) r += '-';
            if (i === 11) r += '-';
            r += d[i];
        }
        return r;
    }

    function formatPagIBIG(d) {
        var r = '';
        for (var i = 0; i < d.length; i++) {
            if (i === 4) r += '-';
            if (i === 8) r += '-';
            r += d[i];
        }
        return r;
    }

    var formatters = {
        tin: formatTIN,
        sss: formatSSS,
        philhealth: formatPhilHealth,
        pagibig: formatPagIBIG
    };

    function formatGovId(input) {
        var type = input.getAttribute('data-gov-type');
        var formatter = formatters[type];
        if (!formatter) return;

        var start = input.selectionStart;
        var end = input.selectionEnd;
        var valBefore = input.value;
        var digitsBeforeCursor = digitsOnly(valBefore.substring(0, start)).length;
        var digits = digitsOnly(valBefore);
        var formatted = formatter(digits);

        if (formatted !== valBefore) {
            input.value = formatted;
            var newPos = 0;
            var digitCount = 0;
            while (newPos < formatted.length && digitCount < digitsBeforeCursor) {
                if (formatted[newPos] !== '-') digitCount++;
                newPos++;
            }
            input.setSelectionRange(newPos, newPos);
        }
    }

    document.addEventListener('input', function (e) {
        if (e.target.getAttribute('data-gov-type')) {
            formatGovId(e.target);
        }
    });
})();

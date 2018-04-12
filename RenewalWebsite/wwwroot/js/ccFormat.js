(function ($) {


	/**
	  *
	  */
    var _self = this;


	/**
	  *
	  */
    _self.card_types = [
        {
            name: 'American Express',
            code: 'ax',
            pattern: /^3[47]/,
            valid_length: [15],
            space_length: [2],
            formats: [
                {
                    length: 15,
                    format: 'xxxx xxxxxx xxxxx'
                }
            ]
        }, {
            name: 'Diners Club Carte Blanche',
            code: 'dccb',
            pattern: /^30[0-5]/,
            valid_length: [14],
            space_length: [3],
            formats: [
                {
                    length: 14,
                    format: 'xxxx xxxx xxxx xx'
                }
            ]
        }, {
            name: 'Diners Club International',
            code: 'dci',
            pattern: /^38/,
            valid_length: [14],
            space_length: [3],
            formats: [
                {
                    length: 14,
                    format: 'xxxx xxxx xxxx xx'
                }
            ]
        }, {
            name: 'JCB',
            code: 'jc',
            pattern: /^35(2[89]|[3-8][0-9])/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }, {
            name: 'Visa',
            code: 'vs',
            pattern: /^4/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }, {
            name: 'Mastercard',
            code: 'mc',
            pattern: /^5[1-5]/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }, {
            name: 'maestro',
            code: 'mas',
            pattern: /^(5018|5020|5038|6304|6759|6761|6763)[0-9]{8,15}/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }, {
            name: 'discover',
            code: 'dis',
            pattern: /^(6011|622(12[6-9]|1[3-9][0-9]|[2-8][0-9]{2}|9[0-1][0-9]|92[0-5]|64[4-9])|65)/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }, {
            name: 'unionpay',
            code: 'union',
            pattern: /^(62[0-9])/,
            valid_length: [16, 17, 18, 19],
            space_length: [4],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }, {
                    length: 17,
                    format: 'xxxx xxxx xxxx xxxx x'
                },
                , {
                    length: 18,
                    format: 'xxxx xxxx xxxx xxxx xx'
                }, {
                    length: 19,
                    format: 'xxxx xxxx xxxx xxxx xxx'
                }
            ]
        }, {
            name: 'Mastercardseries',
            code: 'mcs',
            pattern: /^22[0-9]/,
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        },
        {
            name: 'unknown',
            code: 'unknown',
            pattern: '',
            valid_length: [16],
            space_length: [3],
            formats: [
                {
                    length: 16,
                    format: 'xxxx xxxx xxxx xxxx'
                }
            ]
        }
    ];

	/**
	  *
	  */
    this.isValidLength = function (cc_num, card_type) {
        for (var i in card_type.valid_length) {
            if (cc_num.length <= card_type.valid_length[i]) {
                return true;
            }
        }
        return false;
    };

	/**
	  *
	  */
    this.getCardType = function (cc_num) {
        for (var i in _self.card_types) {
            var card_type = _self.card_types[i];
            if (cc_num.match(card_type.pattern) && _self.isValidLength(cc_num, card_type)) {
                return card_type;
            }
        }
    };

    this.getCardLength = function (cc_num) {
        for (var i in _self.card_types) {
            var card_type = _self.card_types[i];
            if (cc_num.match(card_type.pattern) && _self.isValidLength(cc_num, card_type)) {
                return card_type;
            }
        }
    };

	/**
	  *
	  */
    this.getCardFormatString = function (cc_num, card_type) {
        for (var i in card_type.formats) {
            var format = card_type.formats[i];
            if (cc_num.length <= format.length) {
                return format.format;
            }
        }
    };

	/**
	  *
	  */
    this.formatCardNumber = function (cc_num, card_type) {
        var numAppendedChars = 0;
        var formattedNumber = '';

        if (!card_type) {
            return cc_num;
        }

        var cardFormatString = _self.getCardFormatString(cc_num, card_type);
        for (var i = 0; i < cc_num.length; i++) {
            cardFormatIndex = i + numAppendedChars;
            if (!cardFormatString || cardFormatIndex >= cardFormatString.length) {
                return cc_num;
            }

            if (cardFormatString.charAt(cardFormatIndex) !== 'x') {
                numAppendedChars++;
                formattedNumber += cardFormatString.charAt(cardFormatIndex) + cc_num.charAt(i);
            } else {
                formattedNumber += cc_num.charAt(i);
            }
        }

        return formattedNumber;
    };

	/**
	  *
	  */
    this.monitorCcFormat = function ($elem) {
        var cc_num = $elem.val().replace(/\D/g, '');
        var card_type = _self.getCardType(cc_num);
        if (card_type != null) {
            var card_length = Math.max.apply(Math, card_type.valid_length);
            var space_length = Math.max(card_type.space_length);
            ($elem).attr('maxlength', (card_length + space_length));
        }
        $elem.val(_self.formatCardNumber(cc_num, card_type));
        _self.addCardClassIdentifier($elem, card_type);
    };

	/**
	  *
	  */
    this.addCardClassIdentifier = function ($elem, card_type) {
        var classIdentifier = 'cc_type_unknown';
        if (card_type) {
            classIdentifier = 'cc_type_' + card_type.code;
        }

        if (!$elem.hasClass(classIdentifier)) {
            var classes = '';
            for (var i in _self.card_types) {
                classes += 'cc_type_' + _self.card_types[i].code + ' ';
            }
            $elem.removeClass(classes + 'cc_type_unknown');
            $elem.addClass(classIdentifier);
        }
    };

	/**
	  *
	  */
    $(function () {
        $(document).find('.ccFormatMonitor').each(function () {
            var $elem = $(this);
            if ($elem.is('input')) {
                $elem.bind('input', function () {
                    _self.monitorCcFormat($elem);
                });
                _self.monitorCcFormat($elem);
            }
        });
    });
}(jQuery));

D = {}; B = {}

limitUI = ->
    a = (o, a...) ->
        for v, i in a when i % 2 is 0
            for j in [v..a[i + 1]]
                o[j] = true
        o
    p = a { 9: true, 13: true, 27: true }, 32, 40
    q = a { 8: true, 46: true }, 48, 57, 65, 90, 186, 192, 219, 222
    r = a { 65: true, 67: true, 86: true }, 88, 90

    document.addEventListener 'keydown', (e) ->
        if e.target.tagName in ['INPUT', 'TEXTAREA']
            if e.altKey or not p[e.which] and (e.ctrlKey or not q[e.which]) and
                    (not e.ctrlKey or e.shiftKey or not r[e.which])
                e.preventDefault()
        else if e.altKey or not p[e.which]
            e.preventDefault()

    document.addEventListener 'contextmenu', (e) ->
        unless e.target.tagName in ['INPUT', 'TEXTAREA']
            e.preventDefault()

    document.addEventListener 'wheel', (e) ->
        if e.altKey or e.ctrlKey or e.shiftKey
            e.preventDefault()

o2d = (o) ->
    for k, v of o
        e = document.getElementById k
        if e.tagName is 'SELECT'
            for c, i in e.children when c.value is v
                e.selectedIndex = i
        else if e.type is 'checkbox'
            e.checked = v
        else
            e.value = v

d2o = ->
    o = {}
    for e in document.getElementsByTagName 'input'
        o[e.id] = if e.type is 'checkbox' then e.checked else e.value

    for e in document.getElementsByTagName 'select'
        o[e.id] = e.children[e.selectedIndex].value
    o

window.onCompleted = (f, b) ->
    D = d2o()

    document.getElementById('restore').addEventListener 'click', ->
        o2d D

    document.forms[0].addEventListener 'submit', (e) ->
        e.preventDefault()
        o = d2o()

        B.PreviewC = parseInt o.PreviewC[1..], 16
        delete o.PreviewC

        e = document.getElementById 'ShortcutKey'
        B.ShortcutKey = e.children[e.selectedIndex].value
        delete o.ShortcutKey

        for k, v of o when v is D[k]
            delete o[k]

        window.external.Adapt JSON.stringify(o), JSON.stringify B

    document.getElementById('close').addEventListener 'click', ->
        window.external.Close()

    limitUI()

    try
        o2d JSON.parse f

    B = JSON.parse b
    document.getElementById('PreviewC').value =
        '#' + ('00000' + B.PreviewC.toString 16)[-6..]

    for e in document.getElementById('ShortcutKey').children
        if (e.value | 0) is B.ShortcutKey
            e.selected = true

window.onActivated = ->
    document.getElementsByTagName('section')[0].focus()

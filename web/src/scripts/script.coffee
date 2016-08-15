
B = window.external
C =
    VTool: true, VEditor: true, VMulti: true, VMove: true
    VTask: true, VNumber: true, VTemplate: true

    REditorW: 388, RMultiW: 283, RMoveW: 100, RNumberW: 226
    RMoveH: 136, RTaskH: 148, RTemplateH: 240

    EditorBorderW: 8, EditorMinW: 32, WheelableEditorBorder: false

    TaskMarginX: 0, TaskPaddingX: 16, TaskRows: 7, TaskW: 48, VTaskTitle: 1

    TemplateArea: 8042, TemplateMarginX: 12
    TemplatePaddingX: 16, TemplateRows: 7

    IsPreview: true

    AutoHah: true, HahFontS: 16, IsSplitHah: false, IsSplitHahTemplate: false

    VHMulti: true, VHMove: true, VHTask: true, VHNumber: true, VHTemplate: true

update = (a...) ->
    for v in a
        update[v]()

draggable = (r, s, distance, start, drag, stop) ->
    t = d = f = false

    $(document).on 'mousemove', (e) ->
        e.from = f
        if d
            drag.call t, e
        else if f and (Math.abs(e.pageX - f.pageX) >= distance or
                Math.abs(e.pageY - f.pageY) >= distance)
            d = true
            $('body').removeClass 'not-dragging'
            start.call t, e
            drag.call t, e

    $(document).on 'mouseup', (e) ->
        e.from = f
        stop.call t, e
        d = f = false
        $('body').addClass 'not-dragging'

    $(r).on 'mousedown', s, (e) ->
        if e.which is 1
            t = @
            f = e
            false

eachNode = ($n, b, h, v) ->
    $c = $n.children '.node'
    g = $c.map(-> +$(@).css('flex').split(' ')[0]).toArray()
    d = g.reduce (d, g) -> d + g
    b = if $n.hasClass 'column' then b.height else b.width
    r = g.reduce ((r, g) -> r - b * g // d), b
    a = g.map (g, i) -> [i, +('.' + ('' + b * g / d).split('.')[1])]
    a.sort (p, q) -> q[1] - p[1]
    (if $n.hasClass 'column' then v else h) $c, a.reduce ((s, a, i) ->
        s[a[0]] = b * g[a[0]] // d + (i < r); s), []

window.onActivated = ->
    $(document).focus()
    if $('.number input').is ':focus'
        $('.number input').select()

window.serialize = ->
    a = ($n) ->
        unless $n.hasClass 'root'
            g = +$n.css('flex').split(' ')[0]
        if $n.hasClass 'leaf'
            g: g
        else
            c: $n.children('.node').map(-> a $ @).toArray()
            d: $n.hasClass 'column'
            g: g
    JSON.stringify
        bounds: $('.multi .selected').data 'bounds'
        number: $('.number .display').map(-> $(@).data 'bounds').toArray()
        template: $('.template .root').map(-> a $ @).toArray()
        validKey: C.ValidKey

grow = ->
    a = (s, g) -> $(s).css 'flex', g + ' 0'
    b = (s, g, h) -> a(s, g).parent().css 'flex', h + ' 0'
    a '.editor', C.REditorW
    a '.multi', C.RMultiW
    b '.move', C.RMoveW, C.RMoveH
    b '.task', C.RTaskH, C.RMultiW + C.RMoveW + 2
    b '.number', C.RNumberW, C.RMoveH + C.RTaskH + 2
    a '.template', C.RTemplateH

hide = ->
    a = ($n) ->
        if $n.is 'menu, article'
            v = (v for k, v of C when k[0] is 'V' and
                $n.hasClass k[1..].toLowerCase())[0]
        else
            v = $n.children().toArray().reduce ((b, c) -> a($ c) or b), false
        unless v
            $n.appendTo '.hidden'
        v
    a $ '.c-root'

limitUI = ->
    a = (o, a...) ->
        for v, i in a when i % 2 is 0
            for j in [v..a[i + 1]]
                o[j] = true
        o
    p = a { 8: true, 9: true, 46: true }, 32, 40
    q = a {}, 48, 57, 65, 90, 186, 192, 219, 222
    r = 65: true, 67: true, 86: true, 88: true

    document.addEventListener 'keydown', (e) ->
        if (not (e.target.tagName in ['INPUT', 'TEXTAREA'])) or
                e.altKey or not p[e.which] and (e.ctrlKey or not q[e.which]) and
                (not e.ctrlKey or e.shiftKey or not r[e.which])
            e.preventDefault()

    document.addEventListener 'contextmenu', (e) ->
        unless e.target.tagName in ['INPUT', 'TEXTAREA']
            e.preventDefault()

    document.addEventListener 'wheel', (e) ->
        if e.altKey or e.ctrlKey or e.shiftKey
            e.preventDefault()

window.onCompleted = (f, u, v) ->
    try
        for k, p of JSON.parse f
            C[k] = if typeof p is 'string' then p | 0 else p
    try
        u = JSON.parse u
    catch
        u = {}

    grow(); hide(); initTool(); initEditor(); initMove(); initTask v
    initMulti u; initNumber u; initTemplate u; initHah u

    $(window).resize ->
        update 'editorRoot', 'multi', 'move', 'task', 'template'

    limitUI()

    $(document).on 'keydown', (e) ->
        $t = $ '.scroll'
        if e.key is 'Esc'
            B.Hide()
        else if $(e.target).is 'input'
        else if e.key is 'PageUp'
            $t.scrollTop $t.scrollTop() - $t[0].clientHeight
        else if e.key is 'PageDown'
            $t.scrollTop $t.scrollTop() + $t[0].clientHeight
        else if e.key is 'End'
            $t.scrollTop 9000
        else if e.key is 'Home'
            $t.scrollTop 0

# -----------------------------------------------------------------------------
# tool

[window.pushUndo, window.popUndo] = do ->
    a = []
    [
        (f = -> ) ->
            a.push f
            if a.length > 50
                a.shift()
        ->
            if a.length
                a.pop()()
            B.PopUndo()
    ]

initTool = ->
    $(document).on 'keyup', (e) ->
        if e.key is 'Alt'
            if C.VTool
                $t = $ '.tool .menu'; o = $t.offset()
                B.ShowMenu o.left + $t.width() / 2, o.top + $t.height() / 2
            else
                B.ShowMenu 0, 0

    $(document).on 'keydown', (e) ->
        if e.ctrlKey and e.key is 'z'
            popUndo()
        else if e.ctrlKey and e.key is 's'
            B.Save()

    return unless C.VTool

    $('.tool .menu').on 'click', ->
        $t = $ '.tool .menu'; o = $t.offset()
        B.ShowMenu o.left + $t.width() / 2, o.top + $t.height() / 2

    $('.tool .undo').on 'click', popUndo
    $('.tool .save').on 'click', -> B.Save()

# -----------------------------------------------------------------------------
# editor

update.editorRoot = ->
    return unless C.VEditor

    b = $('.multi .selected').data 'workingArea'
    r = b.width / b.height; $t = $ '.editor .container'

    if $t.width() / $t.height() < r
        $t.addClass 'column'
        $('.editor .root').css 'flex', '0 ' + $t.width() / r + 'px'
    else
        $t.removeClass 'column'
        $('.editor .root').css 'flex', '0 ' + $t.height() * r + 'px'

update.editorLeaf = ->
    return unless C.VEditor

    if C.WheelableEditorBorder
        a = ($n, b) ->
            if $n.hasClass 'leaf'
                $n.text b.width + ' x ' + b.height
            else
                eachNode($n, b, ($c, ss) ->
                    a $c.eq(i), width: s, height: b.height for s, i in ss
                ($c, ss) ->
                    a $c.eq(i), width: b.width, height: s for s, i in ss)
        a $('.editor .root'), $('.multi .selected').data 'workingArea'
    else
        $('.editor .leaf').empty()

pushEditor = ->
    $t = $('.editor .root').clone()
    pushUndo ->
        $('.editor .root').replaceWith $t
        update 'editorRoot', 'editorLeaf'
    B.PushUndo()

clearEditor = ->
    if C.VEditor and not $('.editor .root').hasClass 'leaf'
        pushEditor()
        $('.editor .root').empty().removeClass('column').addClass 'leaf'
        update 'editorLeaf'

addEditor = ->
    if C.VEditor and C.VTemplate
        pushTemplate()
        $t = $('.editor .root').clone().css 'flex', ''
        (if $t.hasClass 'leaf' then $t else $t.find '.leaf').empty()
        $('<div>').addClass('element').append($t).appendTo '.template .float'
        update 'template', 'hah'

window.splitEditor = $.noop
splitEditor_ = (n, c) ->
    $t = $ @
    $b = (i) -> if i then $('<div>').addClass 'border' else ''
    $l = -> $('<div>').addClass 'node leaf'

    pushEditor()
    if $t.hasClass('root') or c isnt $t.parent().hasClass 'column'
        $t.empty().removeClass('leaf').toggleClass 'column', c
        for i in [0...n]
            $t.append $b(i), $l().css 'flex', '60 0'
    else
        g = $t.css('flex').split(' ')[0] / n
        for i in [0...n]
            $t.before $b(i), $l().css 'flex', g + ' 0'
        $t.remove()
    update 'editorLeaf'

window.deleteEditorBorder = $.noop
deleteEditorBorder_ = ->
    $t = $ @; $p = $t.prev(); $n = $t.next()

    pushEditor()
    if $t.parent().children().length is 3
        $t.parent().empty().removeClass('column').addClass 'leaf'
    else
        $p.empty().removeClass('column').addClass 'leaf'
            .css('flex', +$p.css('flex').split(' ')[0] +
                +$n.css('flex').split(' ')[0] + ' 0')
        $n.remove()
        $t.remove()
    update 'editorLeaf'

dragEditorBorder = ->
    p = n = c = 0

    draggable('.editor', '.border', 0, ->
        pushEditor()
        $t = $ @
        if $t.parent().hasClass 'column'
            $('body').addClass 'dragging-ec'
            p = $t.prev().height()
            n = $t.next().height()
        else
            $('body').addClass 'dragging-e'
            p = $t.prev().width()
            n = $t.next().width()
        c = $t.prev().css('flex').split(' ')[0] / p

    (e) ->
        return if p + n < C.EditorMinW * 2

        $t = $ @
        if $t.parent().hasClass 'column'
            d = e.pageY - e.from.pageY
        else
            d = e.pageX - e.from.pageX
        d += Math.max(0, C.EditorMinW - p - d) +
            Math.min 0, n - d - C.EditorMinW

        $t.prev().css 'flex', (p + d) * c + ' 0'
        $t.next().css 'flex', (n - d) * c + ' 0'
        update 'editorLeaf'

    -> $('body').removeClass 'dragging-e dragging-ec')

wheelEditorBorder = ->
    return unless C.WheelableEditorBorder

    a = ($n, d) ->
        if $n.hasClass 'leaf'
            +$n.text().split(' x ')[+d]
        else if d is $n.hasClass 'column'
            $n.children('.node').toArray().reduce ((s, c) -> s + a $(c), d), 0
        else
            a $n.children().first(), d

    $('.editor').on 'mousewheel', '.border', (e) ->
        $t = $ @; c = $t.parent().hasClass 'column'
        if c
            p = $t.prev().height()
            n = $t.next().height()
        else
            p = $t.prev().width()
            n = $t.next().width()
        if Math.abs(e.deltaX) > Math.abs e.deltaY
            d = e.deltaX
        else
            d = e.deltaY
        d = if p <= C.EditorMinW then Math.min 0, d else d
        d = if n <= C.EditorMinW then Math.max 0, d else d
        p = a $t.prev(), c
        n = a $t.next(), c
        r = $t.prev().css('flex').split(' ')[0] / p

        $t.prev().css 'flex', (p - d) * r + ' 0'
        $t.next().css 'flex', (n + d) * r + ' 0'
        update 'editorLeaf'
        B.SetCursor $t.offset().left + $t.width() / 2,
            $t.offset().top + $t.height() / 2

initEditor = ->
    return unless C.VEditor

    $('.editor .clear').on 'click', clearEditor
    $('.editor .add').on 'click', addEditor

    $('.editor').on 'contextmenu', '.leaf', (e) ->
        window.splitEditor = (n, c) ->
            splitEditor_.call e.target, n, c

        a = (w) ->
            for i in [2..4]
                C.EditorMinW * i + C.EditorBorderW * (i - 1) <= w
        B.ShowEditorContextMenu e.pageX, e.pageY, JSON.stringify(
            a($(e.target).width()).concat a $(e.target).height())

    $('.editor').on 'contextmenu', '.border', (e) ->
        window.deleteEditorBorder = ->
            deleteEditorBorder_.call e.target

        $(e.target).css 'cursor', 'defalut'
        B.ShowDeleteContextMenu e.pageX, e.pageY, 'deleteEditorBorder'
        $(e.target).css 'cursor', ''

    dragEditorBorder()
    wheelEditorBorder()

# -----------------------------------------------------------------------------
# multi

update.multi = ->
    return unless C.VMulti

    $e = $ '.multi .element'

    fs = x: 0, y: 0, right: 0, bottom: 0
    $e.each ->
        b = $(@).data 'bounds'
        fs.x = Math.min fs.x, b.x
        fs.y = Math.min fs.y, b.y
        fs.right = Math.max fs.right, b.x + b.width
        fs.bottom = Math.max fs.bottom, b.y + b.height
    fs.width = fs.right - fs.x
    fs.height = fs.bottom - fs.y

    $c = $ '.multi .container'
    if ($c.width() - 2) / ($c.height() - 2) < fs.width / fs.height
        r = ($c.width() - 2) / fs.width
    else
        r = ($c.height() - 2) / fs.height

    $e.each ->
        b = $(@).data 'bounds'
        x = (b.x - fs.x) * r + 2 | 0
        y = (b.y - fs.y) * r + 2 | 0

        $(@).css(
            left: ($c.width() - fs.width * r - 2) / 2 + x | 0
            top: ($c.height() - fs.height * r - 2) / 2 + y | 0
            width: (b.x + b.width - fs.x) * r - x | 0
            height: (b.y + b.height - fs.y) * r - y | 0)

window.onChangedMulti = (bs, ws) ->
    bs = JSON.parse bs
    ws = JSON.parse ws

    b = $('.multi .selected').data 'bounds'

    $c = $('.multi .container').empty()
    for w, i in ws
        $('<div>').addClass 'element'
            .data(bounds: bs[i], workingArea: w).appendTo $c

    $e = $ '.multi .element'
    $t = $e.toArray().reduce ((r, e) -> r or ((k for k of b).reduce ((r, k) ->
        r and b[k] is $(e).data('bounds')[k]), true) and $ e), false
    ($t or $e.first()).click()

    update 'multi', 'hah'

initMulti = (u) ->
    b = u.bounds or {}
    $('.multi .selected').data 'bounds',
        x: b.x | 0, y: b.y | 0, width: b.width | 0, height: b.height | 0

    $('.multi').on 'click', '.element:not(.selected)', ->
        $('.multi .selected').removeClass 'selected'
        $(@).addClass 'selected'
        update 'editorRoot', 'editorLeaf', 'template'

    return unless C.VMulti

    a = (x) ->
        $e = $ '.multi .element'
        $e.eq((x + $e.index $ '.multi .selected') %% $e.length).click()

    $('.multi').on 'mousewheel', (e) ->
        a e.deltaY

    $(document).on 'keydown', (e) ->
        unless $(e.target).is('input') or e.key isnt 'Spacebar'
            a 1 - e.shiftKey * 2
            if C.IsPreview and $('.hover').length
                dropTask $('.dragged'), $('.hover'), true

# -----------------------------------------------------------------------------
# move

update.move = initMove = ->
    return unless C.VMove

    $t = $ '.move .container'
    $t.toggleClass 'column', $t.width() < $t.height()
    $('.move .root').css 'flex',
        '0 ' + Math.min($t.width(), $t.height()) + 'px'

# -----------------------------------------------------------------------------
# task

update.task = ->
    ew = C.TaskW + C.TaskMarginX
    cw = $('.task .container').width() -
        C.TaskPaddingX * 2 + C.TaskMarginX - 17

    $('.task .float').width Math.min ew * C.TaskRows, cw - cw % ew

window.onChangedTask = (ls, bs) ->
    ls = JSON.parse ls
    bs = JSON.parse bs

    if C.VTaskTitle is 1
        duplication = {}
        for b in bs
            duplication[b] or= duplication[b]?

    $c = $('.task .float').empty()
    for b, i in bs
        $d = $('<div>').addClass('element').data 'long', ls[i]
            .css 'background-image', "url(data:image/png;base64,#{b})"
        $c.append $d

        if C.VTaskTitle is 2 or C.VTaskTitle is 1 and duplication[b]
            $d.append $('<span>').text B.GetTaskTitle ls[i]
    update 'hah'

toggleFilter = ->
    $t = $ '.task .filter'; t = $t.text(); b = t.indexOf('ON') is -1
    $t.text t.replace ['ON', 'OFF'][+b], ['OFF', 'ON'][+b]
    B.ToggleFilter()

hoverTask = ->
    $(document).on 'mouseenter', '.not-dragging .task .element', ->
        $('.task h2').text B.GetTaskTitle $(@).data 'long'

    $(document).on 'mouseleave', '.not-dragging .task .element', ->
        $t = $ '.task .selected'
        if $t.length
            $('.task h2').text B.GetTaskTitle $t.data 'long'

dragTask = ->
    $d = $()
    draggable('.task', '.element', 4, ->
        $(@).addClass 'dragged'
        $('body').addClass 'dragging-t'
        $d = $ '.leaf, .number input, .number .display'

    (e) ->
        $f = $ '.hover'
        $t = $(document.elementFromPoint e.pageX, e.pageY).filter $d

        if not ($f.length or $t.length)
        else if not $f.length
            $t.addClass 'hover'
            if C.IsPreview
                dropTask $(@), $t, true

        else if not $t.length
            $f.removeClass 'hover'
            if C.IsPreview
                B.HidePreview()

        else if $f[0] isnt $t[0]
            $f.removeClass 'hover'
            $t.addClass 'hover'
            if C.IsPreview
                dropTask $(@), $t, true

    ->
        $t = $ '.hover'

        $(@).removeClass 'dragged'
        $t.removeClass 'hover'
        $('body').removeClass 'dragging-t'
        if C.IsPreview
            B.HidePreview()

        dropTask $(@), $t, false)

dropTask = ($t, $d, p) ->
    b = $('.multi .selected').data 'workingArea'

    if not $t.length
    else if $d.hasClass 'root'
        B.MaximizeTask $t.data('long'), b.x, b.y, b.width, b.height, p

    else if $d.is '.editor .leaf, .template .leaf'
        a = ($d, $n, b) ->
            if not $n.hasClass 'leaf'
                eachNode($n, b, ($c, ws) -> ws.reduce(((r, w, i) ->
                    [r[0] or a($d, $c.eq(i), x: r[1], y: b.y, width: w, height: b.height), r[1] + w]), [false, b.x])[0]
                ($c, hs) -> hs.reduce(((r, h, i) ->
                    [r[0] or a($d, $c.eq(i), x: b.x, y: r[1], width: b.width, height: h), r[1] + h]), [false, b.y])[0])
            else if $d[0] is $n[0]
                b
            else
                false
        c = a $d, $d.closest('.root'), b
        B.SetTaskBounds $t.data('long'), c.x, c.y, c.width, c.height, p

    else if $d.is('.move .leaf, .number .display') or
            $d.is('.number input') and setInputNumber p
        c = $d.data 'bounds'
        B.SetTaskBoundsStr $t.data('long'),
            c.x, c.y, c.width, c.height, b.x, b.y, b.width, b.height, p

    else if p
        B.HidePreview()

showTask = ($t) ->
    $w = $ '.task .container'
    w =
        x: $w.offset().left, y: $w.offset().top
        width: $w[0].clientWidth, height: $w[0].clientHeight
    t =
        x: $t.offset().left, y: $t.offset().top
        width: C.TaskW, height: C.TaskW

    if w.width < t.width
        $w.scrollLeft $w.scrollLeft() + t.x + t.width / 2 - w.x - w.width / 2
    else
        $w.scrollLeft $w.scrollLeft() + Math.min(0, t.x - w.x) +
            Math.max 0, t.x + t.width - w.x - w.width

    if w.height < t.height
        $w.scrollTop $w.scrollTop() + t.y + t.height / 2 - w.y - w.height / 2
    else
        $w.scrollTop $w.scrollTop() + Math.min(0, t.y - w.y) +
            Math.max 0, t.y + t.height - w.y - w.height

window.clickTask = (l) ->
    $('.task .element').each ->
        $t = $ @
        if $t.data('long') is l
            showTask $t
            B.ClickTask $t.offset().left + C.TaskW / 2,
                $t.offset().top + C.TaskW / 2

selectTask = ($t) ->
    if $t.length
        $('.task .selected').removeClass 'selected'
        $('.task h2').text B.GetTaskTitle $t.addClass('selected').data 'long'
        showTask $t

moveTask = (x) ->
    $e = $ '.task .element'; $s = $ '.task .selected'
    if $e.length
        selectTask $e.eq (x + $e.index $s) %% $e.length

keyTask = ->
    $(document).on 'keydown', (e) ->
        $t = $ '.task .selected'
        if $(e.target).is('input') or not $t.length

        else if e.key is 'Enter'
            B.SecondTask $t.data 'long'

        else if e.key is 'Left'
            moveTask -1
        else if e.key is 'Up'
            moveTask -$('.task .float').width() / (C.TaskW + C.TaskMarginX)
        else if e.key is 'Right'
            moveTask 1
        else if e.key is 'Down'
            moveTask $('.task .float').width() / (C.TaskW + C.TaskMarginX)

        else if e.key is 'Apps' or e.key is ';'
            B.ShowTaskContextMenu $t.offset().left + C.TaskW / 2,
                $t.offset().top + C.TaskW / 2, $t.data 'long'

initTask = (v) ->
    update 'task'

    $('.task .filter').text 'フィルター ' + ['OFF', 'ON'][+v]
        .on 'click', toggleFilter

    $('.task').on 'click', '.element', ->
        B.SecondTask $(@).data 'long'

    $('.task').on 'contextmenu', '.element', (e) ->
        B.ShowTaskContextMenu e.pageX, e.pageY, $(e.target).data 'long'

    hoverTask()
    dragTask()
    keyTask()

# -----------------------------------------------------------------------------
# number

pushNumber = ->
    $t = $('.number .element').clone()
    pushUndo ->
        $('.number .container').empty().append $t
        update 'hah'
    B.PushUndo()

validateNumber = (s) ->
    for i in [0...4]
        s[i] = if isNaN +s[i] then '' else $.trim s[i]
    for i in [2...4]
        s[i] = if s[i].indexOf('.') is -1 and s[i] <= 0 then '' else s[i]

    if not (s[0] or s[1] or s[2] or s[3])
        '入力が空、もしくは数値ではありません。'

    else if not s[...4].reduce ((e, s) ->
            e and (s.indexOf('.') is -1 or 0 <= +s <= 1)), true
        '少数が 0.0 以上 1.0 以下の範囲外です。'

    else
        null

setInputNumber = (h) ->
    $t = $ '.number input'
    s = $t.val().split ','
    e = validateNumber s

    if not e
        $t.data 'bounds',
            x: s[0], y: s[1], width: s[2], height: s[3], name: $.trim s[4]
    else if not h
        B.ShowMes e
    not e

window.getErrorAndDropTask = (s) ->
    $t = $ '.task .selected'
    s = s.split ','
    e = validateNumber s

    if not $t.length
        e = ''
    else if not e
        b = $('.multi .selected').data 'workingArea'
        e = B.GetErrorAndSetTaskBoundsStr $t.data('long'),
            s[0], s[1], s[2], s[3], b.x, b.y, b.width, b.height, false
        if C.AutoHah and not e
            moveTask 1
    e

createNumber = (b) ->
    $('<div>').addClass('element').append(
        $('<div>').addClass('display').attr 'data-bounds', JSON.stringify b
            .text b.name or [b.x, b.y, b.width, b.height].join ', ')

addNumber = ->
    if C.VNumber
        $t = $('.number input').select()
        if setInputNumber false
            pushNumber()
            createNumber($t.data 'bounds').prependTo $ '.number .container'
            update 'hah'

window.deleteNumber = $.noop

initNumber = (u) ->
    $t = $ '.number .container'
    for b in u.number or []
        $t.append createNumber
            x: b.x + '', y: b.y + ''
            width: b.width + '', height: b.height + '', name: b.name + ''

    $(document).on 'keydown', (e) ->
        if C.VNumber and e.key is 'Tab' and not $('.number input').is ':focus'
            $('.number input').select()
        else if C.ValidKey and e.ctrlKey and e.key is 'n'
            B.ShowNumber()

    return unless C.VNumber

    $('.number .add').on 'click', addNumber

    $('.number').on 'click', '.display', (e) ->
        b = $(e.target).data 'bounds'
        $('.number input').val [b.x, b.y, b.width, b.height, b.name].join ', '
            .select()

    $('.number').on 'contextmenu', '.display', (e) ->
        $t = $ e.target
        window.deleteNumber = ->
            pushNumber()
            $t.parent().remove()
            update 'hah'

        B.ShowDeleteContextMenu e.pageX, e.pageY, 'deleteNumber'

    $('.number .container').sortable
        axis: 'y'
        containment: '.number'
        distance: 4
        tolerance: 'pointer'
        start: -> $('body').removeClass 'not-dragging'
        stop: -> $('body').addClass 'not-dragging'
        update: -> update 'hah'

    $('.number input').on 'keydown', (e) ->
        if e.which is 13
            addNumber()

# -----------------------------------------------------------------------------
# template

update.template = ->
    return unless C.VTemplate

    b = $('.multi .selected').data 'workingArea'
    eiw = Math.sqrt(C.TemplateArea * b.width / b.height) | 0
    eih = Math.sqrt(C.TemplateArea * b.height / b.width) | 0
    eow = eiw + C.TemplateMarginX + 4
    cw = $('.template .container').width() -
        C.TemplatePaddingX * 2 + C.TemplateMarginX - 17

    $('.template .element').width(eiw).height eih
    $('.template .float').width Math.min eow * C.TemplateRows, cw - cw % eow

pushTemplate = ->
    $t = $('.template .element').clone()
    pushUndo ->
        $('.template .float').empty().append $t
        update 'template', 'hah'
    B.PushUndo()

window.deleteTemplate = $.noop

initTemplate = (u) ->
    a = (n) ->
        if (n.c or []).length
            $t = $('<div>').addClass('node').toggleClass 'column', not not n.d
                .css 'flex', +n.g + ' 0'
            for c, i in n.c or []
                $t.append (if i then $('<div>').addClass 'border' else ''), a c
            $t
        else
            $('<div>').addClass('node leaf').css 'flex', +n.g + ' 0'

    $t = $ '.template .float'
    for n in u.template or []
        $t.append($('<div>').addClass 'element'
            .append a(n).addClass('root').css 'flex', '')

    return unless C.VTemplate

    $t.on 'contextmenu', '.leaf, .border', (e) ->
        window.deleteTemplate = ->
            pushTemplate()
            $(e.target).closest('.element').remove()
            update 'hah'

        B.ShowDeleteContextMenu e.pageX, e.pageY, 'deleteTemplate'

    $t.sortable
        containment: '.template .container'
        distance: 4
        tolerance: 'pointer'
        start: -> $('body').removeClass 'not-dragging'
        stop: -> $('body').addClass 'not-dragging'
        update: -> update 'hah'

    return unless C.VEditor

    $t.on 'click', '.leaf, .border', ->
        $t = $(@).closest('.root').clone()
        (if $t.hasClass 'leaf' then $t else $t.find '.leaf').empty()
        $e = $('.editor .root').clone().css 'flex', ''
        (if $e.hasClass 'leaf' then $e else $e.find '.leaf').empty()

        return if $t[0].outerHTML is $e[0].outerHTML

        pushEditor()
        $('.editor .root').replaceWith $t
        update 'editorRoot', 'editorLeaf'

# -----------------------------------------------------------------------------
# hah

hint = ''

calcHah = (n, c) ->
    i = 0; pi = 1
    while c[Math.min i, c.length - 1].length * pi < n
        pi *= c[Math.min i, c.length - 1].length
        i += 1
    b = (0 for j in [0..i])

    for i in [0...n]
        h = b.reduce ((h, v, j) ->
            s = c[Math.min j, c.length - 1]
            if j and j is b.length - 1 and n < pi * 2 and n % pi <= i < pi
                h
            else if j < c.length or j % 2 isnt c.length % 2
                h + s[v]
            else
                h + s[s.length - v - 1]), ''
        for v, j in b
            b[j] = (v + 1) % c[Math.min j, c.length - 1].length
            break if b[j]
        h

update.hah = ->
    hint = ''
    $('header kbd').empty().show()
    $('.container kbd').remove()

    unless C.ValidKey
        $('.task .selected').removeClass 'selected'
        $('.number .display').css 'padding-left', ''
        return

    a = (c, n) ->
        $e = $ ".#{n} .element"
        return unless $e.length and C['VH' + n[0].toUpperCase() + n[1..]]

        h = calcHah $e.length, ['ASDFJKL']
        if h.length is 1
            h[0] = ''

        $(".#{n} header kbd").text if C.IsSplitHah then c else ''
        $e.each (i) ->
            $(@).prepend($ '<kbd>'
                .text (if C.IsSplitHah then '' else c) + h[i])
    a 'M', 'multi'
    a 'T', 'task'
    a 'N', 'number'

    if C.VHMove
        $('.move header kbd').text if C.IsSplitHah then 'E' else ''
        $('.move .leaf').each (i) ->
            $(@).prepend($ '<kbd>'
                .text (if C.IsSplitHah then '' else 'E') + 'UIOJKLM,.'[i])

    if C.VHTemplate
        $e = $ '.template .element'
        h = calcHah $e.length, ['ASDFJKL']

        $e.each (i) ->
            $(@).prepend($ '<kbd>'
                .text if C.IsSplitHahTemplate then h[i] else '')

            $l = $(@).find '.leaf'
            g = calcHah $l.length, ['ASDFJKL']
            if g.length is 1
                g[0] = ''

            $l.each (j) ->
                $(@).prepend($ '<kbd>'
                    .text (if C.IsSplitHahTemplate then '' else h[i]) + g[j])

    selectTask $('.task .element').first()

    if C.VHNumber
        $('.number .display').css('padding-left', C.HahFontS *
            ($('.number .element kbd').first().text().length / 2 + 1.5))
    else
        $('.number .display').css 'padding-left', ''

window.validHah = -> C.ValidKey

window.toggleHah = ->
    C.ValidKey = not C.ValidKey
    update 'hah'
    B.Save()

keyHah = ->
    $(document).on 'keydown', (e) ->
        return unless C.ValidKey and ',.adefijklmnostu'.indexOf(e.key) > -1 and
             not e.ctrlKey and not $(e.target).is 'input'

        k = e.key.toUpperCase()
        $h = $ 'article:not(.template) .container kbd:visible, ' +
            '.template .leaf kbd:visible'

        b = $h.toArray().reduce ((b, t) ->
            if b instanceof $
                return b

            $t = $ t; $l = $t.closest 'article'
            if $l.hasClass 'template'
                h = $t.closest('.element').children('kbd').text() + $t.text()
            else
                h = $l.find('header kbd').text() + $t.text()

            if h[hint.length] is k and h.length is hint.length + 1
                $t
            else
                b or h[hint.length] is k), false

        if b instanceof $
            hint = ''
            $('kbd').show()

            if b.is '.multi kbd'
                b.parent().click()
            else if b.is '.task kbd'
                selectTask b.parent()
            else if b.is '.number kbd'
                dropTask $('.task .selected'), b.next(), false
                if C.AutoHah
                    moveTask 1
            else
                dropTask $('.task .selected'), b.parent(), false
                if C.AutoHah
                    moveTask 1

        else if b
            $h.each ->
                $t = $ @; $l = $t.closest 'article'
                if $l.hasClass 'template'
                    $p = $t.closest('.element').children 'kbd'
                else
                    $p = $l.find 'header kbd'

                unless ($p.text() + $t.text())[hint.length] is k
                    $t.hide()
                    $p.hide()
            hint += k

backspaceHah = ->
    $(document).on 'keydown', (e) ->
        return unless hint.length and e.key is 'Backspace' and
            not $(e.target).is 'input'

        hint = hint[...-1]
        a = ($t, $p) ->
            if ($p.text() + $t.text()).indexOf(hint) is 0
                $t.show()
                $p.show()

        $('article:not(.template) .container kbd').each ->
            a $(@), $(@).closest('article').find 'header kbd'

        $('.template .leaf kbd').each ->
            a $(@), $(@).closest('.element').children 'kbd'

initHah = (u) ->
    C.ValidKey = not not u.validKey

    $(document).on 'keydown', (e) ->
        if e.ctrlKey and e.key is 'f'
            toggleHah()

    keyHah()
    backspaceHah()

# -----------------------------------------------------------------------------

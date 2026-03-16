window.corkboardInterop = {
    _activeDrag: null,

    initDraggable: function (elementId, handleId, dotNetRef) {
        const handle = document.getElementById(handleId);
        if (!handle) return;

        const onPointerDown = (e) => {
            e.preventDefault();
            e.stopPropagation();

            const element = document.getElementById(elementId);
            if (!element) return;

            const rect = element.getBoundingClientRect();
            const scrollContainer = element.closest('.corkboard');
            const scrollLeft = scrollContainer ? scrollContainer.scrollLeft : 0;
            const scrollTop = scrollContainer ? scrollContainer.scrollTop : 0;

            window.corkboardInterop._activeDrag = {
                element: element,
                dotNetRef: dotNetRef,
                offsetX: e.clientX - rect.left + scrollLeft - element.offsetLeft + rect.left,
                offsetY: e.clientY - rect.top + scrollTop - element.offsetTop + rect.top,
                startX: e.clientX,
                startY: e.clientY
            };

            element.classList.add('dragging');
            document.addEventListener('pointermove', window.corkboardInterop._onPointerMove);
            document.addEventListener('pointerup', window.corkboardInterop._onPointerUp);
        };

        handle.addEventListener('pointerdown', onPointerDown);
    },

    _onPointerMove: function (e) {
        const drag = window.corkboardInterop._activeDrag;
        if (!drag) return;

        e.preventDefault();

        const scrollContainer = drag.element.closest('.corkboard');
        const scrollLeft = scrollContainer ? scrollContainer.scrollLeft : 0;
        const scrollTop = scrollContainer ? scrollContainer.scrollTop : 0;

        let newX = e.clientX + scrollLeft - drag.offsetX;
        let newY = e.clientY + scrollTop - drag.offsetY;

        // Prevent negative positions
        newX = Math.max(0, newX);
        newY = Math.max(0, newY);

        drag.element.style.left = newX + 'px';
        drag.element.style.top = newY + 'px';
    },

    _onPointerUp: function (e) {
        const drag = window.corkboardInterop._activeDrag;
        if (!drag) return;

        drag.element.classList.remove('dragging');
        document.removeEventListener('pointermove', window.corkboardInterop._onPointerMove);
        document.removeEventListener('pointerup', window.corkboardInterop._onPointerUp);

        const newX = parseFloat(drag.element.style.left) || 0;
        const newY = parseFloat(drag.element.style.top) || 0;

        drag.dotNetRef.invokeMethodAsync('OnPositionChanged', newX, newY);
        window.corkboardInterop._activeDrag = null;
    }
};

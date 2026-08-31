"use strict";

(() => {
    const status = document.getElementById("realtime-status");

    if (!status) {
        return;
    }

    const roomId = status.dataset.roomId;

    const date = status.dataset.date;

    const rows = new Map(
        Array.from(document.querySelectorAll("[data-slot-id]"))
            .map(row => [row.dataset.slotId.toLowerCase(), row])
    );

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(status.dataset.hubUrl)
        .withAutomaticReconnect()
        .build();

    function markSlotAsBooked(slotId) {
        const row = rows.get(slotId.toLowerCase());

        if (!row) {
            return false;
        }

        const checkbox = row.querySelector(
            'input[name="TimeSlotIds"]'
        );

        if (checkbox) {
            checkbox.checked = false;
            checkbox.disabled = true;
        }

        const badge = row.querySelector(".badge");

        if (badge) {
            badge.textContent = "Booked";
            badge.classList.remove("bg-success");
            badge.classList.add("bg-danger");
        }

        return true;
    }

    connection.on("SlotsBooked", message => {
        if (message.meetingRoomId.toLowerCase() !== roomId.toLowerCase()) {
            return;
        }

        let updated = false;

        for (const slotId of message.timeSlotIds) {
            if (markSlotAsBooked(slotId)) {
                updated = true;
            }
        }

        if (updated) {
            status.textContent =
                "Schedule updated: some slots have been booked.";
        }
    });

    async function synchronizeSchedule() {
        const bookedSlotIds = await connection.invoke(
            "GetBookedSlotIds",
            roomId,
            date
        );

        for (const slotId of bookedSlotIds) {
            markSlotAsBooked(slotId);
        }
    }

    async function joinRoom() {
        await connection.invoke("JoinRoom", roomId);
        await synchronizeSchedule();
        status.textContent = "Live updates connected.";
    }

    connection.onreconnecting(() => {
        status.textContent =
            "Connection lost. Reconnecting to live updates…";
    });

    connection.onreconnected(async () => {
        try {
            await joinRoom();
        } catch (error) {
            status.textContent =
                "Could not restore live updates. Refresh the page.";
            console.error("Could not rejoin the room.", error);
        }
    });

    connection.onclose(() => {
        status.textContent =
            "Live updates disconnected. Refresh the page.";
    });

    async function start() {
        try {
            await connection.start();
            await joinRoom();
        } catch (error) {
            status.textContent =
                "Live updates unavailable. Refresh the page to retry.";
            console.error("Could not connect to live updates.", error);
        }
    }

    void start();
})();
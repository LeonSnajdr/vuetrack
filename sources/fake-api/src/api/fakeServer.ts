import express, { Request, Response } from "express";
import cors from "cors";

// Types
type Branded<T, Brand> = T & { __brand: Brand };

type TimeEntryId = Branded<string, "timeEntryId">;
type TimeEntrySuggestionId = Branded<string, "timeEntryId">;

type TimeEntryContract = {
    id: TimeEntryId;
    user: string;
    taskId: string;
    startTime: Date;
    endTime: Date;
};

type TimeEntryCreateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
};

type TimeEntryUpdateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
};

type TimeEntrySuggestionContract = {
    id: TimeEntrySuggestionId;
    taskId: string;
    startTime: Date;
    endTime: Date;
};

type TimeEntrySuggestionUpdateContract = {
    taskId: string;
    startTime: Date;
    endTime: Date;
};

// Seed data helpers
const getWeekStart = (): Date => {
    const now = new Date();
    const day = now.getDay();
    const diff = now.getDate() - day + (day === 0 ? -6 : 1); // Monday
    const monday = new Date(now.setDate(diff));
    monday.setHours(0, 0, 0, 0);
    return monday;
};

const createSuggestionDate = (dayOffset: number, hour: number): Date => {
    const date = new Date(getWeekStart());
    date.setDate(date.getDate() + dayOffset);
    date.setHours(hour, 0, 0, 0);
    return date;
};

// In-memory stores
let timeEntries: TimeEntryContract[] = [];
let timeEntrySuggestions: TimeEntrySuggestionContract[] = [
    {
        id: "suggestion-1" as TimeEntrySuggestionId,
        taskId: "PROJ-101",
        startTime: createSuggestionDate(0, 9),  // Monday 9am
        endTime: createSuggestionDate(0, 11),   // Monday 11am
    },
    {
        id: "suggestion-2" as TimeEntrySuggestionId,
        taskId: "PROJ-102",
        startTime: createSuggestionDate(1, 14), // Tuesday 2pm
        endTime: createSuggestionDate(1, 16),   // Tuesday 4pm
    },
    {
        id: "suggestion-3" as TimeEntrySuggestionId,
        taskId: "PROJ-103",
        startTime: createSuggestionDate(2, 10), // Wednesday 10am
        endTime: createSuggestionDate(2, 12),   // Wednesday 12pm
    },
    {
        id: "suggestion-4" as TimeEntrySuggestionId,
        taskId: "PROJ-101",
        startTime: createSuggestionDate(3, 13), // Thursday 1pm
        endTime: createSuggestionDate(3, 15),   // Thursday 3pm
    },
];

// Config
const DELAY_MIN_MS = 1000;
const DELAY_MAX_MS = 4000;
const FAILURE_RATE = 0.05; // 0-1, probability of request failing (0.1 = 10%)

// Utils
const generateId = (): string => crypto.randomUUID();
const delay = (): Promise<void> => new Promise((resolve) => setTimeout(resolve, DELAY_MIN_MS + Math.random() * (DELAY_MAX_MS - DELAY_MIN_MS)));
const shouldFail = (): boolean => Math.random() < FAILURE_RATE;

const toISOResponse = <T extends Record<string, unknown>>(obj: T): T => {
    const result = { ...obj } as Record<string, unknown>;
    if (result.startTime instanceof Date) result.startTime = result.startTime.toISOString();
    if (result.endTime instanceof Date) result.endTime = result.endTime.toISOString();
    return result as T;
};

// App
const app = express();
app.use(cors());
app.use(express.json());

// Failure simulation middleware
app.use((_req, res, next) => {
    if (shouldFail()) {
        res.status(500).json({ error: "Simulated server error" });
        return;
    }
    next();
});

// TimeEntry endpoints
app.get("/timeEntries", async (_req: Request, res: Response) => {
    await delay();
    res.json(timeEntries.map(toISOResponse));
});

app.post("/timeEntries", async (req: Request, res: Response) => {
    await delay();
    const body: TimeEntryCreateContract = req.body;
    const entry: TimeEntryContract = {
        id: generateId() as TimeEntryId,
        user: "fake-user",
        taskId: body.taskId,
        startTime: new Date(body.startTime),
        endTime: new Date(body.endTime),
    };
    timeEntries.push(entry);
    res.status(201).json(toISOResponse(entry));
});

app.put("/timeEntries/:id", async (req: Request, res: Response) => {
    await delay();
    const { id } = req.params;
    const body: TimeEntryUpdateContract = req.body;
    const index = timeEntries.findIndex((e) => e.id === id);
    if (index === -1) {
        res.status(404).json({ error: "Not found" });
        return;
    }
    timeEntries[index] = {
        ...timeEntries[index],
        taskId: body.taskId,
        startTime: new Date(body.startTime),
        endTime: new Date(body.endTime),
    };
    res.json(toISOResponse(timeEntries[index]));
});

app.delete("/timeEntries/:id", async (req: Request, res: Response) => {
    await delay();
    const { id } = req.params;
    const index = timeEntries.findIndex((e) => e.id === id);
    if (index === -1) {
        res.status(404).json({ error: "Not found" });
        return;
    }
    timeEntries.splice(index, 1);
    res.status(204).send();
});

// TimeEntrySuggestion endpoints
app.get("/timeEntrySuggestions", async (_req: Request, res: Response) => {
    await delay();
    res.json(timeEntrySuggestions.map(toISOResponse));
});

app.put("/timeEntrySuggestions/:id", async (req: Request, res: Response) => {
    await delay();
    const { id } = req.params;
    const body: TimeEntrySuggestionUpdateContract = req.body;
    const index = timeEntrySuggestions.findIndex((e) => e.id === id);
    if (index === -1) {
        res.status(404).json({ error: "Not found" });
        return;
    }
    timeEntrySuggestions[index] = {
        ...timeEntrySuggestions[index],
        taskId: body.taskId,
        startTime: new Date(body.startTime),
        endTime: new Date(body.endTime),
    };
    res.json(toISOResponse(timeEntrySuggestions[index]));
});

app.delete("/timeEntrySuggestions/:id", async (req: Request, res: Response) => {
    await delay();
    const { id } = req.params;
    const index = timeEntrySuggestions.findIndex((e) => e.id === id);
    if (index === -1) {
        res.status(404).json({ error: "Not found" });
        return;
    }
    timeEntrySuggestions.splice(index, 1);
    res.status(204).send();
});

// Start
app.listen(5001, () => {
    console.log("Fake API running on http://localhost:5001");
});

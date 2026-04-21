using Mezon.Sdk.Domain;

namespace TaskManagement.Bot.Application.Commands.Complain;

public static class ComplainFormBuilder
{
    private static readonly object[] DurationOptions = new object[]
    {
        new { label = "1 hour",  value = "1"  },
        new { label = "2 hours",  value = "2"  },
        new { label = "3 hours",  value = "3"  },
        new { label = "4 hours",  value = "4"  },
        new { label = "5 hours",  value = "5"  },
        new { label = "6 hours",  value = "6"  },
        new { label = "12 hours", value = "12" },
        new { label = "24 hours", value = "24" },
        new { label = "48 hours", value = "48" },
        new { label = "72 hours", value = "72" },
    };

    private static readonly object[] TypeOptions = new object[]
    {
        new { label = "📅 Request deadline extension", value = "RequestExtend" },
        new { label = "❌ Request task cancellation", value = "RequestCancel" }
    };

    public static ChannelMessageContent BuildComplainForm(object[] taskOptions)
    {
        var fields = new List<object>
        {
            new
            {
                name = "📋 Task",
                value = string.Empty,
                inputs = new
                {
                    id = "complain_task_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Select task...",
                        options = taskOptions
                    }
                }
            },
            new
            {
                name = "📌 Type",
                value = string.Empty,
                inputs = new
                {
                    id = "complain_type_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Select complaint type...",
                        options = TypeOptions
                    }
                }
            },
            new
            {
                name = "📝 Reason",
                value = string.Empty,
                inputs = new
                {
                    id = "complain_reason",
                    type = 3,
                    component = new
                    {
                        id = "complain_reason_component",
                        placeholder = "Enter reason...",
                        defaultValue = "",
                        type = "text",
                        textarea = true
                    }
                }
            },
            new
            {
                name = "⏰ Time extension (only for deadline extension requests)",
                value = string.Empty,
                inputs = new
                {
                    id = "extend_duration",
                    type = 2,
                    component = new
                    {
                        placeholder = "Select extension duration...",
                        options = DurationOptions
                    }
                }
            }
        };

        var embed = new
        {
            title = "📋 Submit Complaint",
            description = "Please fill in all complaint information.",
            color = "#5865F2",
            fields = fields.ToArray()
        };

        return new ChannelMessageContent
        {
            Text = "Please fill out the complaint form:",
            Embed = new object[] { embed },
            Components = new object[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new { type = 1, id = "complain_submit", component = new { label = "✅ Submit", style = 3 } },
                        new { type = 1, id = "complain_cancel", component = new { label = "❌ Cancel", style = 4 } }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildApproveForm(object[] complainOptions)
    {
        var fields = new List<object>
        {
            new
            {
                name = "📋 Select complaint to review",
                value = string.Empty,
                inputs = new
                {
                    id = "approve_complain_select",
                    type = 2,
                    component = new
                    {
                        placeholder = "Choose a complaint...",
                        options = complainOptions
                    }
                }
            },
            new
            {
                name = "📝 Rejection reason (only if rejecting)",
                value = string.Empty,
                inputs = new
                {
                    id = "reject_reason",
                    type = 3,
                    component = new
                    {
                        id = "reject_reason_component",
                        placeholder = "Enter rejection reason...",
                        defaultValue = "",
                        type = "text",
                        textarea = true
                    }
                }
            }
        };

        var embed = new
        {
            title = "🔍 Review Complaints",
            description = "Select a complaint to approve or reject.",
            color = "#5865F2",
            fields = fields.ToArray()
        };

        return new ChannelMessageContent
        {
            Text = "Please select a complaint to review:",
            Embed = new object[] { embed },
            Components = new object[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new { type = 1, id = "complain_approve_submit", component = new { label = "✅ Approve", style = 3 } },
                        new { type = 1, id = "complain_reject_submit",  component = new { label = "❌ Reject",  style = 4 } },
                        new { type = 1, id = "approve_cancel", component = new { label = "Cancel", style = 2 } }
                    }
                }
            }
        };
    }

    public static ChannelMessageContent BuildReviewForm(int complainId, string taskTitle, string type, string reason, string requestedBy)
    {
        var embed = new
        {
            title = $"🔍 Review Complaint: {taskTitle}",
            description = $"**From:** {requestedBy}\n**Type:** {type}\n**Reason:** {reason}",
            color = "#5865F2",
            fields = new object[]
            {
                new
                {
                    name = "📝 Rejection reason (if rejected)",
                    value = string.Empty,
                    inputs = new
                    {
                        id = "reject_reason",
                        type = 3,
                        component = new
                        {
                            id = $"reject_reason_{complainId}",
                            placeholder = "Enter rejection reason...",
                            defaultValue = "",
                            type = "text",
                            textarea = false
                        }
                    }
                }
            }
        };

        return new ChannelMessageContent
        {
            Text = "Please review this complaint:",
            Embed = new object[] { embed },
            Components = new object[]
            {
                new
                {
                    type = 1,
                    components = new object[]
                    {
                        new { type = 1, id = $"complain_approve_{complainId}", component = new { label = "✅ Approve", style = 3 } },
                        new { type = 1, id = $"complain_reject_{complainId}", component = new { label = "❌ Reject", style = 4 } }
                    }
                }
            }
        };
    }
}
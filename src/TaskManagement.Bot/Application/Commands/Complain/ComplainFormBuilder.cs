// TaskManagement.Bot.Application.Commands.Complain.ComplainFormBuilder.cs
using Mezon.Sdk.Domain;
using TaskManagement.Bot.Application.Services;

namespace TaskManagement.Bot.Application.Commands.Complain;

/// <summary>
/// Builds interactive forms for complain workflow.
/// </summary>
public static class ComplainFormBuilder
{
    // Duration options for extending deadline
    private static readonly object[] DurationOptions = new object[]
    {
        new { label = "1 giờ",  value = "1"  },
        new { label = "2 giờ",  value = "2"  },
        new { label = "3 giờ",  value = "3"  },
        new { label = "4 giờ",  value = "4"  },
        new { label = "5 giờ",  value = "5"  },
        new { label = "6 giờ",  value = "6"  },
        new { label = "12 giờ", value = "12" },
        new { label = "24 giờ", value = "24" },
        new { label = "48 giờ", value = "48" },
        new { label = "72 giờ", value = "72" },
    };

    // Complain type options
    private static readonly object[] TypeOptions = new object[]
    {
        new { label = "RequestExtend", value = "RequestExtend" },
        new { label = "RequestCancel",     value = "RequestCancel" }
    };

    /// <summary>
    /// Single form: Select task, complain type, reason and duration (if extend).
    /// </summary>
    public static ChannelMessageContent BuildComplainForm(object[] taskOptions)
    {
        var fields = new List<object>
        {
            new
            {
                name = "Task",
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
                name = "Type",
                value = string.Empty,
                inputs = new
                {
                    id = "complain_type_select",
                    type = 2, 
                    component = new
                    {
                        placeholder = "Select type...",
                        options = TypeOptions
                    }
                }
            },
            new
            {
                name = "Reason",
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
                name = "Time extension (only requests to postpone the deadline)",
                value = string.Empty,
                inputs = new
                {
                    id = "extend_duration",
                    type = 2, 
                    component = new
                    {
                        placeholder = "Choose a time...",
                        options = DurationOptions
                    }
                }
            }
        };

        var embed = new
        {
            title = "📋 Complain",
            description = "Fill in all the complaint information.",
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
                        new
                        {
                            type = 1, 
                            id = "complain_submit",
                            component = new { label = "Submit", style = 3 }
                        },
                        new
                        {
                            type = 1,
                            id = "complain_cancel",
                            component = new { label = "Cancel", style = 4 }
                        }
                    }
                }
            }
        };
    }

    /// <summary>
    /// PM review form: approve or reject a complain.
    /// </summary>
    public static ChannelMessageContent BuildReviewForm(int complainId, string taskTitle, string type, string reason, string requestedBy)
    {
        var embed = new
        {
            title = $"🔍 Review complaints: {taskTitle}",
            description = $"From: {requestedBy} | Type: {type}\nReason: {reason}",
            color = "#5865F2",
            fields = new object[]
            {
                new
                {
                    name = "Reason for rejection (if rejected)",
                    value = string.Empty,
                    inputs = new
                    {
                        id = "reject_reason",
                        type = 3,
                        component = new
                        {
                            id = "reject_reason_component",
                            placeholder = "Enter the reason for refusal...",
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
            Text = "Review complaints:",
            Embed = new object[] { embed },
            Components = new object[]
            {
                new
                {
                    type = 1, // ACTION_ROW
                    components = new object[]
                    {
                        new
                        {
                            type = 1, // BUTTON
                            id = $"complain_approve_{complainId}",
                            component = new { label = "✅ Approve", style = 3 }
                        },
                        new
                        {
                            type = 1, // BUTTON
                            id = $"complain_reject_{complainId}",
                            component = new { label = "❌ Reject", style = 4 }
                        }
                    }
                }
            }
        };
    }
}